# TODO: add new columns to live database
# TODO: deduplicate mbartistdetails list when loading it
# TODO: check if aliases can be used for better naming predictions
# TODO: fix that the popup to enter new values is floating in space
# TODO: Add handling for files without musicbrainz data
# TODO: check if data was changed and post changes to DB
# TODO: move buttons to bottom
# TODO: make separate table for each song
# TODO: save_file_metadata works already but immediately returns for debugging reasons
# TODO: Infer album artist from file path
# TODO: make gui nicer looking
# TODO: colors -> grey out rows where included is disabled
# TODO: colors -> have specific color for values loaded from the db
# TODO: colors -> highlight colors that are different from the current id tag / were edited

import json
import httpx
import os
import asyncio
from typing import List, Optional
from mutagen import id3

class Alias:
	def __init__(self, name: str, type: str, locale: Optional[str], begin: Optional[str], end: Optional[str], type_id: str, ended: bool, sort_name: str, primary: bool):
		self.name = name
		self.type = type
		self.locale = locale
		self.begin = begin
		self.end = end
		self.type_id = type_id
		self.ended = ended
		self.sort_name = sort_name
		self.primary = primary

	def __str__(self):
		return	f"{self.type} ({self.locale}):{self.name}"
	
	def __repr__(self):
		return	f"{self.type} ({self.locale}):{self.name}"

	@classmethod
	def from_dict(cls, data: dict):
		return cls(
			name=data.get("name"),
			type=data.get("type"),
			locale=data.get("locale"),
			begin=data.get("begin"),
			end=data.get("end"),
			type_id=data.get("type-id"),
			ended=data.get("ended"),
			sort_name=data.get("sort-name"),
			primary=data.get("primary", False)
		)

class MbArtistDetais:
	def __init__(self, name: str, type: str, disambiguation: str, sort_name: str, id: str, aliases: List[Alias], type_id: str, joinphrase: Optional[str]):
		self.include: bool = True
		self.name = name
		self.type = type
		self.disambiguation = disambiguation
		self.sort_name = sort_name
		self.mbid = id
		self.aliases = aliases
		self.type_id = type_id
		self.joinphrase = joinphrase
		self.custom_name = sort_name
		self.custom_original_name = name
		self.id: int = -1

	def __str__(self):
		return	f"{self.name}"
	
	def __repr__(self):
		return	f"{self.name}"
	
	def update_from_customization(self, data: dict) -> None:
		self.include = data['include']
		self.custom_name = data['name']
		self.custom_original_name = data['originalName']
		self.id = data['id']


	@classmethod
	def from_dict(cls, data: dict, track_list: List['MbArtistDetais']):
		aliases = [Alias.from_dict(alias) for alias in data.get("aliases", [])]
		track = cls(
			name=data.get("name"),
			type=data.get("type"),
			disambiguation=data.get("disambiguation"),
			sort_name=data.get("sort_name"),
			id=data.get("id"),
			aliases=aliases,
			type_id=data.get("type_id"),
			joinphrase=data.get("joinphrase", "")
		)
		track_list.append(track)
		# Handle nested relations by flattening them
		for relation in data.get("relations", []):
			cls.from_dict(relation, track_list)

	@staticmethod
	def parse_json(json_str: str) -> List['MbArtistDetais']:
		data = json.loads(json_str)
		track_list = []
		for item in data:
			MbArtistDetais.from_dict(item, track_list)
		return track_list

class TrackDetails:
	tag_mappings = {
		'TIT2': {"property": "title", "frame": id3.TIT2},
		'TPE1': {"property": "artist", "frame": id3.TPE1},
		'TALB': {"property": "album", "frame": id3.TALB},
		'TPE2': {"property": "album_artist", "frame": id3.TPE2},
		'TIT1': {"property": "grouping", "frame": id3.TIT1},
		'TOAL': {"property": "original_album", "frame": id3.TOAL},
		'TOPE': {"property": "original_artist", "frame": id3.TOPE},
		'TPE3': {"property": "original_title", "frame": id3.TPE3}
	}

	def __init__(self, file_path: str, manager):
		self.file_path = file_path
		self.manager = manager
		self.title = None
		self.artist = None
		self.album = None
		self.album_artist = None
		self.grouping = None
		self.original_album = None
		self.original_artist = None
		self.original_title = None

		
	def __str__(self):
		return	f"{self.title}"
	
	def __repr__(self):
		return	f"{self.title}"
		
	async def read_file_metadata(self) -> None:
		loop = asyncio.get_event_loop()
		self.id3 = await loop.run_in_executor(None, lambda: id3.ID3(self.file_path))
		for tag, mapping  in self.tag_mappings.items():
			value = self.id3.get(tag, [''])[0]
			setattr(self, mapping["property"], value)

		# the artist_relations array is not a specific ID3 tag but is stored as text in the general purpose TXXX frame
		artist_relations_frame = next((frame for frame in self.id3.getall("TXXX") if frame.desc == 'artist_relations_json'), None)
		if artist_relations_frame:
			artist_relations = artist_relations_frame.text[0]
			self.mbArtistDetails = await self.manager.parse_mbartist_json(artist_relations)

	def save_file_metadata(self) -> None:
		return
		file_changed: bool = False

		for tag, mapping  in self.tag_mappings.items():
			value = getattr(self, mapping["property"])
			file_value = self.id3.get(tag, [''])[0]

			if value:
				if file_value != value:
					# the current property has a value and it's different from the value in the file
					# self.frame_mapping[tag](encoding=3, text=value)
					self.id3[tag] = mapping["frame"](encoding=3, text=value)
					file_changed = True
			else:
				if file_value:
					# the current property doesn't have a value, but the file doesn't
					# pop is executed immediately, so file_changed doesn't need to be set
					self.id3.pop(tag, None)

		if file_changed:
			self.id3.save(self.file_path)

class TrackManager:
	MBARTIST_API_ENDPOINT = "api/mbartist"
	MBARTIST_API_PORT = 23409
	MBARTIST_API_DOMAIN = "localhost"

	def __init__(self):
		self.tracks: list[TrackDetails] = []
		self.mb_artist_data: dict[MbArtistDetais] = {}

	async def load_directory(self, directory: str) -> None:
		self.directory = directory
		self.get_mp3_files()
		await self.read_file_metadata()

	def get_mp3_files(self) -> None:
		for root, dirs, files in os.walk(self.directory):
			for file in files:
				if file.endswith(".mp3"):
					file_path = os.path.join(root, file)
					self.tracks.append(TrackDetails(file_path, self))
	
	async def save_files(self) -> None:
		loop = asyncio.get_event_loop()
		await asyncio.gather(*(loop.run_in_executor(None, track.save_file_metadata) for track in self.tracks))

	async def read_file_metadata(self) -> None:
		await asyncio.gather(*(track.read_file_metadata() for track in self.tracks))

	async def parse_mbartist_json(self, artist_relations_json: str) -> list[MbArtistDetais]:
		artist_details = MbArtistDetais.parse_json(artist_relations_json)
		returnObj: List[MbArtistDetais] = []
		for artist in artist_details:
			if artist.mbid not in self.mb_artist_data:
				self.mb_artist_data[artist.mbid] = artist

				mb_artist_details = await self.get_mbartist_customization(artist.mbid)
				if(None != mb_artist_details):
					artist.update_from_customization(mb_artist_details)

			returnObj.append(self.mb_artist_data[artist.mbid])

		return returnObj

	async def get_mbartist_customization(self, mbid:str) -> dict:
		async with httpx.AsyncClient() as client:
			response = await client.get(f"http://localhost:23409/api/mbartist/mbid/{mbid}")
			match response.status_code:
				case 200:
					artist_info = response.json()
					return artist_info
				case 404:
					return None
				case _:
					raise Exception(f"Failed to fetch artist data for MBID {mbid}: {response.status_code}")

	async def persist_mbartist_customizations(self) -> None:
		for artist in self.mb_artist_data.values():
			customization = await self.get_mbartist_customization(artist.mbid)
			if (None == customization):
				await self.post_mbartist_customization(artist)
			else:
				await self.update_mbartist_customization(customization['id'], artist)

	async def post_mbartist_customization(self, artist:MbArtistDetais) -> None:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.MBARTIST_API_ENDPOINT}"

		data = {
			"MbId": artist.mbid,
			"Name": artist.custom_name,
			"OriginalName": artist.custom_original_name,
			"Include": artist.include
		}

		async with httpx.AsyncClient() as client:
			response = await client.post(f"{endpoint}", json=data)

			if response.is_success:
				return
			
			match response.status_code:
				case 409:
					# TODO: Add some nicer error handler when trying to post duplicates
					raise Exception(f"Failed to post artist data for MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")
				case _:
					raise Exception(f"Failed to post artist data for MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")
	
	async def update_mbartist_customization(self, id:int, artist:MbArtistDetais) -> None:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.MBARTIST_API_ENDPOINT}/id"

		data = {
			"MbId": artist.mbid,
			"Name": artist.custom_name,
			"OriginalName": artist.custom_original_name,
			"Include": artist.include
		}

		async with httpx.AsyncClient() as client:
			response = await client.put(f"{endpoint}/{id}", json=data)

			if response.is_success:
				return
			
			match response.status_code:
				case 404:
					# TODO: Add some nicer error handler when trying to update an artist that doesn't exist
					raise Exception(f"Failed to update artist data for MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")
				case _:
					raise Exception(f"Failed to update artist data for MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")

async def seedData() -> None:
	data = {
    "MbId": "f3688ad9-cd14-4cee-8fa0-0f4434e762bb",
    "Name": "ClariS-Changed",
    "OriginalName": "ClariS-Original-Changed",
    "Include": True
	}
	await send_post_request(data)

async def send_post_request(data) -> None:
	async with httpx.AsyncClient() as client:
		response = await client.post("http://localhost:23409/api/mbartist", json=data)
		print('Status Code:', response.status_code)
		print('Response:', response.text)

async def main() -> None:
	await seedData()
	manager = TrackManager()
	dir = "C:/Users/email_000/Desktop/music/sample/spiceandwolf"
	await manager.load_directory(dir)
	await manager.persist_mbartist_customizations()
	await manager.save_files()

if __name__ == "__main__":
	asyncio.run(main())