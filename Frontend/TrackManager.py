# TODO: add new columns to live database
# TODO: Make type editable. Should be a dropdown with Person, Character, Group
# TODO: deduplicate mbartistdetails list in gridview when loading it
# TODO: add checks for existing data when posting/updating to db
# TODO: check if aliases can be used for better naming predictions
# TODO: fix that the popup to enter new values is floating in space
# TODO: post changes for simple artists
# TODO: check if data was changed and post changes to DB
# TODO: move buttons to bottom
# TODO: make separate table for each song
# TODO: check if mb_artist_data in trackmanager is needed. It could be easier to just make this array when posting updates to the api
# TODO: save_file_metadata works already but immediately returns for debugging reasons
# TODO: Infer album artist from file path
# TODO: make gui nicer looking
# TODO: colors -> grey out rows where included is disabled
# TODO: colors -> have specific color for values loaded from the db
# TODO: colors -> highlight colors that are different from the current id tag / were edited

# simple 1: artist and alias does not exist on server
# simple 2: artist / alias exist on server and is equal to local data
# simple 3: artist / alias exist on server, custom name was changed to artist that doesn't exist on server
# simple 4: artist / alias exist on server, custom name was changed to artist that already exists on server
# mbid 1: mbid does not exist on server
# mbid 2: mbid already exists on server,is equal to local data
# mbid 3: mbid already exists on server, custom name was changed


import hashlib
import os
import re
import json
import httpx
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

class MbArtistDetails:
	def __init__(self, name: str, type: str, disambiguation: str, sort_name: str, id: str, aliases: List[Alias], type_id: str, joinphrase: Optional[str], include:bool = True):
		self.include: bool = include
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

	def update_from_simple_artist_dict(self, data: dict) -> None:
		self.custom_name = data['artist']
		self.custom_original_name = data['name']
		self.id = data['artistId']

	@classmethod
	def from_dict(cls, data: dict, track_list: List['MbArtistDetails']):
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
	def parse_json(json_str: str) -> List['MbArtistDetails']:
		data = json.loads(json_str)
		track_list: List['MbArtistDetails'] = []
		for item in data:
			MbArtistDetails.from_dict(item, track_list)
		return track_list
	

class SimpleArtistDetails(MbArtistDetails):
	def __init__(self, name: str, type: str, disambiguation: str, sort_name: str, id: str, aliases: List[Alias], type_id: str, joinphrase: Optional[str], include: bool = True, product: str = "", product_id: str = ""):
		super().__init__(name, type, disambiguation, sort_name, id, aliases, type_id, joinphrase, include)
		
		self.product = product
		self.product_id = product_id
		self.mbid = SimpleArtistDetails.generate_instance_hash(f"{self.name}-{self.product_id}")

	def __str__(self):
		return f"{self.name}"

	def __repr__(self):
		return f"{self.name}"

	@staticmethod
	def generate_instance_hash(unique_string: str):
		return hashlib.sha256(unique_string.encode()).hexdigest()

	@staticmethod
	def parse_simple_artist(artist_list: str, product: str, product_id: int) -> List['SimpleArtistDetails']:
		split_artists = SimpleArtistDetails.split_artist(artist_list)
		mbartist_list: List['SimpleArtistDetails'] = []
		for artist in split_artists:
			mbartist_list.append(SimpleArtistDetails.from_simple_artist(artist, product, product_id, mbartist_list))
		return mbartist_list

	@classmethod
	def from_simple_artist(cls, artist: str, product: str, product_id: int, artist_list: List['SimpleArtistDetails']):
		extracedArtist = SimpleArtistDetails.extract_cv_artist(artist)
		artistType = "Character"
		artist_include = False

		if(extracedArtist == None):
			artistType = "Person"
			extracedArtist = artist
			artist_include = True

		mbartist = cls(
			include = artist_include,
			name = extracedArtist,
			type = artistType,
			disambiguation=None,
			sort_name=None,
			aliases=[],
			id=None,
			type_id=None,
			joinphrase=None,
			product = product,
			product_id = product_id
		)

		return mbartist
	
	@staticmethod
	def split_artist_list(artist):
		# Split by common delimiters but keep parenthesis intact
		regex = re.compile(r'\s?[,&;、×]\s?|\sand\s|\s?with\s?|\s?feat\.?(?:uring)?\s?')
		return regex.split(artist)

	@staticmethod
	def split_artist_cv(artist):
		# Match and split at parenthesis but keep the parenthesis content as separate elements
		regex = re.compile(r'(\s?[\(|（](?:[Cc][Vv][\:|\.|：]?\s?).*[\)|）])')
		return regex.split(artist)

	@staticmethod
	def split_artist(artist):
		artist_list = SimpleArtistDetails.split_artist_list(artist)

		split_list = []
		for regex_artist in artist_list:
			# For each component, further split it if it contains (CV xxx)
			parts = SimpleArtistDetails.split_artist_cv(regex_artist)
			# Append each part to the final list, removing empty strings
			split_list.extend([p.strip() for p in parts if p.strip()])

		return split_list
	
	@staticmethod
	def extract_cv_artist(cv_artist: str) -> str:
		regex = re.compile(r'\((?:[Cc][Vv][\:|\.|：]?\s?)([^)]+)\)')
		match = regex.search(cv_artist)
		return match.group(1) if match else None

	@staticmethod
	def parse_simple_artist_franchise(track_product, track_album_artist, product_list: dict) -> dict:
		product = {"id": None}

		if not track_product:
			product["name"] = track_album_artist

		# the default product indicating that the track doesn't belong to a franchise is _
		if not product["name"]:
			product["name"] = "_"
		
		resolved_product = [p for p in product_list if p["name"] == product["name"]]

		if resolved_product:
			return resolved_product[0]
			
		product["name"] = "_"
		return product

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
		self.has_mbartist_details: bool = False
		self.product = None
		self.update_file: bool = True
		
	def __str__(self):
		return	f"{self.title}"
	
	def __repr__(self):
		return	f"{self.title}"
		
	async def read_file_metadata(self) -> None:
		self.id3 = await self.read_id3_tags(self.file_path)
		for tag, mapping  in self.tag_mappings.items():
			value = self.id3.get(tag, [''])[0]
			setattr(self, mapping["property"], value)

		# the artist_relations array is not a specific ID3 tag but is stored as text in the general purpose TXXX frame
		artist_relations_frame = next((frame for frame in self.id3.getall("TXXX") if frame.desc == 'artist_relations_json'), None)
		
		if artist_relations_frame:
			artist_relations = artist_relations_frame.text[0]
			self.mbArtistDetails = await self.manager.parse_mbartist_json(artist_relations)
			self.has_mbartist_details = True
		else:
			self.mbArtistDetails = await self.manager.parse_simple_artist(self)
	
	async def read_id3_tags(self, file_path: str):
		loop = asyncio.get_event_loop()
		return await loop.run_in_executor(None, lambda: id3.ID3(file_path))

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
	SIMPLE_ARTIST_API_ENDPOINT = "api/artist"
	SIMPLE_ARTIST_ALIAS_API_ENDPOINT = "api/alias"
	SIMPLE_ARTIST_FRANCHISE_API_ENDPOINT = "api/franchise"
	MBARTIST_API_ENDPOINT = "api/mbartist"
	MBARTIST_API_PORT = 23409
	MBARTIST_API_DOMAIN = "localhost"

	def __init__(self):
		self.tracks: list[TrackDetails] = []
		self.mb_artist_data: dict[MbArtistDetails] = {}

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

	async def parse_simple_artist(self, track: TrackDetails) -> list[SimpleArtistDetails]:
		if not hasattr(self, 'db_products') or not self.db_products:
			self.db_products = await TrackManager.list_simple_artist_franchise()
		
		returnObj: list[SimpleArtistDetails] = []

		product = SimpleArtistDetails.parse_simple_artist_franchise(track.product, track.album_artist, self.db_products)
		track.product = product["name"]
		artist_details = SimpleArtistDetails.parse_simple_artist(track.artist, product["name"], product["id"])

		for artist in artist_details:
			if artist.mbid not in self.mb_artist_data:
				self.mb_artist_data[artist.mbid] = artist

			alias = await TrackManager.get_simple_artist_alias(artist.name, artist.product_id)
			if alias:
				artist.update_from_simple_artist_dict(alias[0])

			returnObj.append(self.mb_artist_data[artist.mbid])

		return returnObj
	

	async def parse_mbartist_json(self, artist_relations_json: str) -> list[MbArtistDetails]:
		artist_details = MbArtistDetails.parse_json(artist_relations_json)
		returnObj: List[MbArtistDetails] = []
		for artist in artist_details:
			if artist.mbid not in self.mb_artist_data:
				self.mb_artist_data[artist.mbid] = artist

				mb_artist_details = await TrackManager.get_mbartist(artist.mbid)
				if mb_artist_details:
					artist.update_from_customization(mb_artist_details)

			returnObj.append(self.mb_artist_data[artist.mbid])

		return returnObj

	async def send_changes_to_db(self) -> None:
		for artist in self.mb_artist_data.values():
			if isinstance(artist, SimpleArtistDetails):
				await TrackManager.send_simple_artist_changes_to_db(None, artist)
			else:
				await TrackManager.send_mbartist_changes_to_db(None, artist)


	@staticmethod
	async def send_mbartist_changes_to_db(track: TrackDetails, artist: MbArtistDetails) -> None:
		customization = await TrackManager.get_mbartist(artist.mbid)
		if (None == customization):
			await TrackManager.post_mbartist(artist)
		else:
			await TrackManager.update_mbartist(customization['id'], artist)

	@staticmethod
	async def send_simple_artist_changes_to_db(track: TrackDetails, artist: SimpleArtistDetails) -> None:
		postedArtist = await TrackManager.post_simple_artist(artist)
		postedAlias = TrackManager.post_simple_artist_alias(postedArtist.id, artist.custom_name, artist.product_id)
	
	@staticmethod
	async def get_mbartist(mbid:str) -> dict:
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

	@staticmethod
	async def list_simple_artist_franchise() -> dict:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.SIMPLE_ARTIST_FRANCHISE_API_ENDPOINT}"

		async with httpx.AsyncClient() as client:
			response = await client.get(f"{endpoint}")
			if response.status_code == 200:
				return response.json()
			else:
				return None

	@staticmethod
	async def get_simple_artist_franchise(name: str = None) -> dict:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.SIMPLE_ARTIST_FRANCHISE_API_ENDPOINT}"

		if not name:
			raise ValueError("No parameters were provided to query.")

		async with httpx.AsyncClient() as client:
			response = await client.get(f"{endpoint}?name={name}")
			if response.status_code == 200:
				return response.json()
			else:
				return None

	@staticmethod
	async def get_simple_artist(self, id:int, name:str) -> dict:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.SIMPLE_ARTIST_API_ENDPOINT}?"
		params = {}

		if id:
			params['id'] = id
		if name:
			params['name'] = name
			
		if not params:
			raise ValueError("No parameters were provided to query.")
		
		query_string = "&".join([f"{key}={value}" for key, value in params.items()])

		async with httpx.AsyncClient() as client:
			response = await client.get(f"{endpoint}?{query_string}")
			if response.status_code == 200:
				return response.json()
			else:
				return None
	
	@staticmethod
	async def get_simple_artist_alias(name: str = None, franchiseId: int = None) -> dict:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.SIMPLE_ARTIST_ALIAS_API_ENDPOINT}"
		params = {}

		if name:
			params['name'] = name
		if franchiseId:
			params['franchiseId'] = franchiseId

		if not params:
			raise ValueError("No parameters were provided to query.")
		
		query_string = "&".join([f"{key}={value}" for key, value in params.items()])

		async with httpx.AsyncClient() as client:
			response = await client.get(f"{endpoint}?{query_string}")
			if response.status_code == 200:
				return response.json()
			else:
				return None

	@staticmethod
	async def post_mbartist(self, artist:MbArtistDetails) -> None:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.MBARTIST_API_ENDPOINT}"

		data = {
			"MbId": artist.mbid,
			"Name": artist.custom_name,
			"OriginalName": artist.custom_original_name,
			"Include": artist.include
		}

		async with httpx.AsyncClient() as client:
			response = await client.post(endpoint, json=data)

			if response.is_success:
				return
			
			match response.status_code:
				case 409:
					raise Exception(f"Artist with MBID {artist.mbid} already exists in DB: {response.text} ({response.status_code} {response.reason_phrase})")
				case _:
					raise Exception(f"Failed to create artist with MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")
	
	@staticmethod
	async def post_simple_artist(self, artist:SimpleArtistDetails) -> None:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.SIMPLE_ARTIST_API_ENDPOINT}"

		data = {
			"Name": artist.custom_name
		}

		async with httpx.AsyncClient() as client:
			response = await client.post(endpoint, json=data)

			if response.is_success:
				return
			
			match response.status_code:
				case 409:
					raise Exception(f"Failed to post artist data for MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")
				case _:
					raise Exception(f"Failed to post artist data for MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")
	
	@staticmethod
	async def post_simple_artist_alias(artist_id: int, name: str, franchise_id: int) -> None:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.SIMPLE_ARTIST_ALIAS_API_ENDPOINT}"
		
		data = {
			"Name": name,
			"artistid": artist_id,
			"franchiseid": franchise_id
		}
		
		async with httpx.AsyncClient() as client:
			response = await client.post(endpoint, json=data)
			
			if response.is_success:
				return
			
			match response.status_code:
				case 409:
					raise Exception(f"Alias with name {name} already exists in DB: {response.text} ({response.status_code} {response.reason_phrase})")
				case _:
					raise Exception(f"Failed to create alias for name {name}: {response.text} ({response.status_code} {response.reason_phrase})")

	@staticmethod
	async def update_mbartist(self, id:int, artist:MbArtistDetails) -> None:
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
					raise Exception(f"Could not find artist with MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")
				case _:
					raise Exception(f"Failed to update artist data for MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")

	@staticmethod
	async def update_simple_artist(self, id:int, artist:SimpleArtistDetails) -> None:
		endpoint = f"http://{TrackManager.MBARTIST_API_DOMAIN}:{TrackManager.MBARTIST_API_PORT}/{TrackManager.SIMPLE_ARTIST_API_ENDPOINT}/id"

		data = {
			"Name": artist.custom_name
		}

		async with httpx.AsyncClient() as client:
			response = await client.put(f"{endpoint}/{id}", json=data)

			if response.is_success:
				return
			
			match response.status_code:
				case 404:
					raise Exception(f"Could not find artist with MBID {artist.id}: {response.text} ({response.status_code} {response.reason_phrase})")
				case _:
					raise Exception(f"Failed to update artist data for MBID {artist.mbid}: {response.text} ({response.status_code} {response.reason_phrase})")


async def seedData() -> None:
	data = {
    "MbId": "f3688ad9-cd14-4cee-8fa0-0f4434e762bb",
    "Name": "ClariS-Changed",
    "OriginalName": "ClariS-Original-Changed",
    "Include": True
	}
	await send_post_request(data, "http://localhost:23409/api/mbartist")

	data = {
    "Name": "_",
	}
	await send_post_request(data, "http://localhost:23409/api/franchise")
	product_default = await send_get_request(f"http://localhost:23409/api/franchise?name={data["Name"]}")

	data = {
    "Name": "TestFranchise1",
	}
	await send_post_request(data, "http://localhost:23409/api/franchise")
	product_testfranchise1 = await send_get_request(f"http://localhost:23409/api/franchise?name={data["Name"]}")

	data = {
    "Name": "TestFranchise2",
	}
	await send_post_request(data, "http://localhost:23409/api/franchise")
	product_testfranchise2 = await send_get_request(f"http://localhost:23409/api/franchise?name={data["Name"]}")

	data = {
    "Name": "idolmaster",
	}
	await send_post_request(data, "http://localhost:23409/api/franchise")
	product_idolmasterproduct = await send_get_request(f"http://localhost:23409/api/franchise?name={data["Name"]}")

	data = {
    "Name": "sandorionSERVER",
	}

	await send_post_request(data, "http://localhost:23409/api/artist")
	artist_sandrion = await send_get_request(f"http://localhost:23409/api/artist?name={data["Name"]}")

	data = {
    "artistId": artist_sandrion["id"],
    "Name": "サンドリオン",
    "franchiseId": product_default["id"],
	}
	artist_sandrionimas = await send_post_request(data, "http://localhost:23409/api/alias")


	data = {
    "Name": "sandorionSERVERIMAS",
	}

	await send_post_request(data, "http://localhost:23409/api/artist")
	artist_sandrionimas = await send_get_request(f"http://localhost:23409/api/artist?name={data["Name"]}")


	data = {
    "artistId": artist_sandrionimas["id"],
    "Name": "サンドリオン",
    "franchiseId": product_idolmasterproduct["id"],
	}
	artist_sandrionimas = await send_post_request(data, "http://localhost:23409/api/alias")


async def send_post_request(data, url) -> None:
	async with httpx.AsyncClient() as client:
		response = await client.post(url, json=data)
		print('url:', url)
		print('Status Code:', response.status_code)
		print('Response:', response.text)

async def send_get_request(url) -> None:
	async with httpx.AsyncClient() as client:
		response = await client.get(url)
		print('url:', url)
		print('Status Code:', response.status_code)
		print('Response:', response.text)
		return response.json()[0]


async def main() -> None:
	# await seedData()
	manager = TrackManager()
	dir = "C:/Users/email_000/Desktop/music/sample/nodetailsmultiple"
	dir = "C:/Users/email_000/Desktop/music/sample/nodetails"
	dir = "C:/Users/email_000/Desktop/music/sample/spiceandwolf"
	dir = "C:/Users/email_000/Desktop/music/"
	await manager.load_directory(dir)
	await manager.send_changes_to_db()
	await manager.save_files()

if __name__ == "__main__":
	asyncio.run(main())