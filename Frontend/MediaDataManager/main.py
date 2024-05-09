import json
import httpx
import os
import asyncio
import tkinter as tk
from typing import List, Optional
from mutagen import id3
# TODO: datetime is just for debuggin purposes, remove it
from datetime import datetime

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
	def __init__(self, file_path: str, manager):
		self.file_path = file_path
		self.manager = manager
		self.title = None
		
	def __str__(self):
		return	f"{self.title}"
	
	def __repr__(self):
		return	f"{self.title}"
		
	async def read_file_metadata(self) -> None:
		loop = asyncio.get_event_loop()
		self.id3 = await loop.run_in_executor(None, lambda: id3.ID3(self.file_path))
		self.title = self.id3.get('TIT2',[''])[0]
		self.artist = self.id3.get('TPE1', [''])[0]
		self.album = self.id3.get('TALB', [''])[0]
		
		artist_relations = ([frame for frame in self.id3.getall("TXXX") if frame.desc == 'artist_relations_json'])[0].text[0]
		self.mbArtistDetails = await self.manager.parse_mbartist_json(artist_relations)

	def save_file_metadata(self) -> None:
		file_changed: bool = False

		self.id3["TSRC"] = id3.TSRC(encoding=3, text=datetime.now().strftime("%H-%M"))
		
		if self.artist:
			self.id3["TEXT"] = id3.TEXT(encoding=3, text=self.artist)

		if file_changed:
			self.id3.save(self.file_path)

class MediaDataManager:
	MBARTIST_API_ENDPOINT = "api/mbartist"
	MBARTIST_API_PORT = 23409
	MBARTIST_API_DOMAIN = "localhost"

	def __init__(self):
		self.tracks: list[TrackDetails] = []
		self.mb_artist_data: dict[MbArtistDetais] = {}

	async def load_directory(self, directory: str) -> None:
		self.directory = directory
		self.load_files()
		await self.read_file_metadata()

	def load_files(self) -> None:
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
		endpoint = f"http://{MediaDataManager.MBARTIST_API_DOMAIN}:{MediaDataManager.MBARTIST_API_PORT}/{MediaDataManager.MBARTIST_API_ENDPOINT}"

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
		endpoint = f"http://{MediaDataManager.MBARTIST_API_DOMAIN}:{MediaDataManager.MBARTIST_API_PORT}/{MediaDataManager.MBARTIST_API_ENDPOINT}/id"

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
	manager = MediaDataManager()
	dir = "C:/Users/email_000/Desktop/music/sample/spiceandwolf"
	await manager.load_directory(dir)
	await manager.persist_mbartist_customizations()
	await manager.save_files()

if __name__ == "__main__":
	asyncio.run(main())



# from tkinter import filedialog, ttk
# from tkinter import messagebox

# class MusicMetadataEditor:
# 	def __init__(self, root):
# 		self.root = root
# 		self.root.title("Music Metadata Editor")

# 		# Layout configuration
# 		self.setup_widgets()
# 		self.setup_table()

# 		# For storing the music files list
# 		self.tracks = []

# 	def setup_widgets(self):
# 		# Frame for the directory selection
# 		self.frame = ttk.Frame(self.root)
# 		self.frame.pack(padx=10, pady=10)

# 		# Button to choose directory
# 		self.btn_select_dir = ttk.Button(self.frame, text="Select Folder", command=self.select_directory)
# 		self.btn_select_dir.pack(side=tk.LEFT)

# 		# Listbox to display files
# 		self.file_listbox = tk.Listbox(self.root, width=100, height=5)
# 		self.file_listbox.pack(padx=10, pady=10)

# 		# Scrollbar for the listbox
# 		self.scrollbar = ttk.Scrollbar(self.root, orient='vertical', command=self.file_listbox.yview)
# 		self.scrollbar.pack(side=tk.RIGHT, fill=tk.Y)
# 		self.file_listbox.config(yscrollcommand=self.scrollbar.set)

# 	def select_directory(self):
# 		# Allow user to select a directory and store file paths
# 		directory = filedialog.askdirectory()
# 		if directory:
# 			self.load_music_files(directory)

# 	def setup_table(self):
# 		# Setting up the Treeview widget for displaying metadata
# 		self.tree = ttk.Treeview(self.root, columns=("Title", "Artist", "Album", "ID", "Type"), show='headings')
# 		self.tree.pack(expand=True, fill=tk.BOTH, padx=10, pady=10)

# 		# Defining the columns
# 		self.tree.heading("Title", text="Track Title")
# 		self.tree.heading("Artist", text="Artist")
# 		self.tree.heading("Album", text="Album")
# 		self.tree.heading("ID", text="Artist ID")
# 		self.tree.heading("Type", text="Artist Type")

# 		# Column widths
# 		self.tree.column("Title", width=200)
# 		self.tree.column("Artist", width=150)
# 		self.tree.column("Album", width=150)
# 		self.tree.column("ID", width=100)
# 		self.tree.column("Type", width=100)

# 		# Button to update metadata
# 		self.update_button = ttk.Button(self.root, text="Update Metadata", command=self.update_metadata)
# 		self.update_button.pack(pady=10)

# 	def update_metadata(self):
# 		# This method will handle the update interaction with the API
# 		for child in self.tree.get_children():
# 			item = self.tree.item(child)
# 			artist_data = json.loads(item['values'][3])
# 			for artist in artist_data:
# 				response = httpx.post(f"http://localhost:8080/api/mbartist?mbId={artist['id']}")
# 				if response.status_code == 200:
# 					print("Update successful for", artist['id'])
# 				else:
# 					print("Failed to update for", artist['id'])

# 	def load_music_files(self, directory):
# 		# Clear the listbox
# 		self.file_listbox.delete(0, tk.END)
# 		self.tracks = []  
# 		self.tree.delete(*self.tree.get_children())  # Clear existing tree view entries

# 		# Walk through the directory
# 		for root, dirs, files in os.walk(directory):
# 			for file in files:
# 				if file.endswith(".mp3"):
# 					file_path = os.path.join(root, file)
# 					trackdetails = TrackDetails(file_path)
# 					self.tracks.append(trackdetails)
# 					self.file_listbox.insert(tk.END, trackdetails.file_path)
# 					trackdetails.read_metadata()

# 		# Load and display metadata for each file
# 		for file_path in self.tracks:
# 			print('a')