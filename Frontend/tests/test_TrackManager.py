import hashlib
import pytest
import httpx
import respx
import json
from unittest.mock import AsyncMock, patch, MagicMock, call
from TrackManager import TrackManager, MbArtistDetails, SimpleArtistDetails, TrackManager, TrackDetails
from mutagen import id3
from mutagen.id3 import TIT2, TPE1, TALB, TPE2, TIT1, TOAL, TOPE, TPE3

api_port = 23409
api_host = "localhost"

expected_person3 = {
    "name": "Person3 Lastname",
    "type": "Person",
    "disambiguation": "",
    "sort_name": "Lastname, Person3",
    "id": "mock-12da-42b2-9fae-3c93b9a3bcdb",
    "aliases": [],
    "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df",
    "relations": []
}

expected_person2 = {
    "name": "Person2 Lastname",
    "type": "Person",
    "disambiguation": "voice actor",
    "sort_name": "Lastname, Person2",
    "id": "mock-d2be-4617-955c-1d0710d03db5",
    "aliases": [],
    "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df",
    "relations": []
}

expected_person1 = {
    "name": "Person1 Lastname",
    "type": "Person",
    "disambiguation": "",
    "sort_name": "Lastname, Person1",
    "id": "mock-d84a-4523-b45c-de3348e968fd",
    "aliases": [
        {
            "locale": "en",
            "name": "Person1AliasEn Lastname",
            "type-id": "894afba6-2816-3c24-8072-eadb66bd04bc",
            "begin": "null",
            "primary": "true",
            "end": "null",
            "sort-name": "Lastname, Person1AliasEn",
            "ended": "false",
            "type": "Artist name"
        },
        {
            "end": "null",
            "locale": "ja",
            "name": "Person1AliasJa Lastname",
            "type-id": "mock-d8f4-4ea6-85a2-cf649203489b",
            "begin": "null",
            "primary": "true",
            "sort-name": "Lastname, Person1AliasJa",
            "ended": "false",
            "type": "Artist name"
        }
    ],
    "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df",
    "relations": [],
    "joinphrase": ")、"
}

expected_character2 = {
    "name": "Character2 Lastname",
    "type": "Character",
    "disambiguation": "Mock Franchise2",
    "sort_name": "Lastname, Character2",
    "id": "mock-3e63-42a5-8251-4dbe07ebc9e2",
    "aliases": [],
    "type_id": "5c1375b0-f18d-3db7-a164-a49d7a63773f",
    "relations": [],
    "joinphrase": "(CV."
}

expected_character1 = {
    "name": "Character1 Lastname",
    "type": "Character",
    "disambiguation": "Mock Franchise1",
    "sort_name": "Lastname, Character1",
    "id": "mock-e7a3-42ac-a08c-3aa896f87bd5",
    "aliases": [],
    "type_id": "5c1375b0-f18d-3db7-a164-a49d7a63773f",
    "relations": [],
    "joinphrase": "(CV."
}


def create_mock_txxx(description, text):
    """
    Returns a mocked id3 frame
    """

    mock_txxx = MagicMock()
    mock_txxx.FrameID = 'TXXX'
    mock_txxx.HashKey = f'TXXX:{description}'
    mock_txxx.desc = description
    mock_txxx.text = text
    return mock_txxx

def create_mock_trackdetails():
    """
    Returns a track details object with dummy values
    """
    track = TrackDetails("/fake/path/file1.mp3", TrackManager())
    track.title = "test title"
    track.artist = "test artist1"
    track.album = "test album"
    track.album_artist = "test album_artist"
    track.grouping = "test grouping"
    track.original_album = "test original_album"
    track.original_artist = "test original_artist"
    track.original_title = "test original_title"
    track.product = "test product"
    track.artist_relations = "test artist_relations"
    
    return track

@pytest.mark.asyncio  
async def test_trackmanager_load_directory(mocker):
    # Arrange
    test_directory = "/fake/directory"
    mocker.patch("os.walk", return_value=[
        (f"{test_directory}/dir1/subdir1", (), ("file1.mp3",)),
        (f"{test_directory}/dir2", ("subdir",), []),
        (f"{test_directory}/dir3", (), ["file1.ogg", "file2.txt"]),
        (f"{test_directory}/dir4", ("subdir",), ["file1.mp3", "file2.mp3"]),
    ])

    manager = TrackManager()
    # Mock read_file_metadata to be an awaitable that does nothing
    mocker.patch.object(manager, 'read_file_metadata', new_callable=AsyncMock)

    # Act
    await manager.load_directory("/fake/directory")

    # Assert
    manager.read_file_metadata.assert_awaited_once()
    assert len(manager.tracks) == 3
    assert any(track.file_path == "/fake/directory/dir1/subdir1\\file1.mp3" for track in manager.tracks)
    assert any(track.file_path == "/fake/directory/dir4\\file1.mp3" for track in manager.tracks)
    assert any(track.file_path == "/fake/directory/dir4\\file2.mp3" for track in manager.tracks)

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_create_track_file_with_artist_json(mock_id3_tags):
    # Arrange1
    reference_track = create_mock_trackdetails()
    reference_track.product = None

    mock_id3_instance = mock_id3_tags({
        "TIT2": [reference_track.title],
        "TPE1": [reference_track.artist],
        "TALB": [reference_track.album],
        "TPE2": [reference_track.album_artist],
        "TIT1": [reference_track.grouping],
        "TOAL": [reference_track.original_album],
        "TOPE": [reference_track.original_artist],
        "TPE3": [reference_track.original_title],
    })
    
    mbid = "mock-93fb-4bc3-8ff9-065c75c4f90a"
    # id3 call for id3.getall("TXXX")
    mock_artist_relations = create_mock_txxx(
        description='artist_relations_json',
        text=[json.dumps([{
            "name": "Firstname Lastname",
            "type": "Person",
            "disambiguation": "", 
            "sort_name": "Lastname, Firstname", 
            "id": mbid, 
            "aliases": [], 
            "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df", 
            "relations": [], 
            "joinphrase": ""
        }])]
    )
    mock_id3_instance.getall.return_value = [mock_artist_relations] 

    # Act
    manager = TrackManager()
    track = TrackDetails("/fake/path/file1.mp3", manager)
    await track.read_file_metadata()

    # Assert
    assert track.title == reference_track.title
    assert track.artist == reference_track.artist
    assert track.album == reference_track.album
    assert track.album_artist == reference_track.album_artist
    assert track.grouping == reference_track.grouping
    assert track.original_album == reference_track.original_album
    assert track.original_artist == reference_track.original_artist
    assert track.original_title == reference_track.original_title
    assert track.product == reference_track.product

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_create_track_file_without_artist_json(respx_mock, mock_id3_tags):
    # Arrange
    reference_track = create_mock_trackdetails()
    reference_track.product = "_"

    # mock individual id3 calls
    mock_id3_instance = mock_id3_tags({
        "TIT2": [reference_track.title],
        "TPE1": [reference_track.artist],
        "TALB": [reference_track.album],
        "TPE2": [reference_track.album_artist],
        "TIT1": [reference_track.grouping],
        "TOAL": [reference_track.original_album],
        "TOPE": [reference_track.original_artist],
        "TPE3": [reference_track.original_title],
    })
    
    # id3.getall("TXXX") returns an empty array to trigger creating simple artist
    mock_id3_instance.getall.return_value = []

    # mock franchise api needed by properly create simple artist objects
    respx_mock.route(
        method="GET", 
        port__in=[api_port], 
        host=api_host, 
        path="/api/franchise"
    ).mock(return_value=httpx.Response(
        200, json=[
            {'id': 1, 'name': '_', 'aliases': []}, 
            {'id': 2, 'name': 'TestFranchise1', 'aliases': []}, 
            {'id': 3, 'name': 'TestFranchise2', 'aliases': []}, 
            {'id': 4, 'name': 'TestFranchise3', 'aliases': []}
        ]
    ))

    # Act
    manager = TrackManager()
    track = TrackDetails("/fake/path/file1.mp3", manager)
    await track.read_file_metadata()

    # Assert
    assert track.title == reference_track.title
    assert track.artist == reference_track.artist
    assert track.album == reference_track.album
    assert track.album_artist == reference_track.album_artist
    assert track.grouping == reference_track.grouping
    assert track.original_album == reference_track.original_album
    assert track.original_artist == reference_track.original_artist
    assert track.original_title == reference_track.original_title
    assert track.product == reference_track.product

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_parse_artist_json_with_nested_objects():
    # Arrange
    track = create_mock_trackdetails()
    manager = track.manager

    expected_character1["relations"] = [ expected_person1 ]
    expected_character2["relations"] = [ expected_person3 ]
    expected_person1["relations"] = [ expected_person2 ]

    # the json object will be deduplicated and flattened, which is why it looks different from the expected list
    expected = [
            expected_character1,
            expected_person1,
            expected_person2,
            expected_character2,
            expected_person3
        ]

    track.artist_relations = json.dumps([
            expected_character1,
            expected_person1,
            expected_character2,
            expected_person3
        ])

    # Act
    await track.create_artist_objects()

    # Assert
    assert len(manager.artist_data) == 5, f"Unexpected number of entries in artist_data"

    artists = track.mbArtistDetails
    assert len(artists) == len(expected), f"Expected {len(expected)} artists, got {len(artists)}"
    for i in range(len(expected)):
        assert artists[i].mbid == expected[i]["id"], f"MBID mismatch at index {i}: expected {expected[i]["id"]}, got {artists[i].mbid}"
        assert artists[i].name == expected[i]["name"], f"name mismatch at index {i}: expected {expected[i]["name"]}, got {artists[i].name}"
        assert artists[i].sort_name == expected[i]["sort_name"], f"sort_name mismatch at index {i}: expected {expected[i]["sort_name"]}, got {artists[i].sort_name}"
        assert artists[i].type == expected[i]["type"], f"type mismatch at index {i}: expected {expected[i]["type"]}, got {artists[i].type}"

@pytest.mark.asyncio
async def test_save_file_metadata_no_changes(mock_id3_tags):
    # Arrange

    track = TrackDetails("/fake/path/file1.mp3", TrackManager())
    track.title = "Same Title"
    track.artist = "Same Artist"
    track.album = "Same Album"
    track.album_artist = "Same Album Artist"
    track.grouping = "Same Grouping"
    track.original_album = "Same Original Album"
    track.original_artist = "Same Original Artist"
    track.original_title = "Same Original Title"
    track.id3 = id3.ID3(track.file_path)

    mock_id3_instance = mock_id3_tags({
        'TIT2': TIT2(encoding=3, text="Same Title"),
        'TPE1': TPE1(encoding=3, text="Same Artist"),
        'TALB': TALB(encoding=3, text="Same Album"),
        'TPE2': TPE2(encoding=3, text="Same Album Artist"),
        'TIT1': TIT1(encoding=3, text="Same Grouping"),
        'TOAL': TOAL(encoding=3, text="Same Original Album"),
        'TOPE': TOPE(encoding=3, text="Same Original Artist"),
        'TPE3': TPE3(encoding=3, text="Same Original Title"),
    })

    # Act
    track.save_file_metadata()

    # Assert
    mock_id3_instance.__setitem__.assert_not_called()
    mock_id3_instance.save.assert_not_called()

@pytest.mark.asyncio
async def test_save_file_metadata_changes(mock_id3_tags):
    # Arrange
    track = TrackDetails("/fake/path/file1.mp3", TrackManager())
    track.title = "New Title"
    track.artist = "New Artist"
    track.album = "New Album"
    track.album_artist = "New Album Artist"
    track.grouping = "New Grouping"
    track.original_album = "New Original Album"
    track.original_artist = "New Original Artist"
    track.original_title = "New Original Title"
    track.id3 = id3.ID3(track.file_path)

    mock_id3_instance = mock_id3_tags({
        'TIT2': TIT2(encoding=3, text="Old Title"),
        'TPE1': TPE1(encoding=3, text="Old Artist"),
        'TALB': TALB(encoding=3, text="Old Album"),
        'TPE2': TPE2(encoding=3, text="Old Album Artist"),
        'TIT1': TIT1(encoding=3, text="Old Grouping"),
        'TOAL': TOAL(encoding=3, text="Old Original Album"),
        'TOPE': TOPE(encoding=3, text="Old Original Artist"),
        'TPE3': TPE3(encoding=3, text="Old Original Title"),
    })

    # Act
    track.save_file_metadata()

    # Assert
    expected_setitem_calls = [
        call('TIT2', TIT2(encoding=3, text=track.title)),
        call('TPE1', TPE1(encoding=3, text=track.artist)),
        call('TALB', TALB(encoding=3, text=track.album)),
        call('TPE2', TPE2(encoding=3, text=track.album_artist)),
        call('TIT1', TIT1(encoding=3, text=track.grouping)),
        call('TOAL', TOAL(encoding=3, text=track.original_album)),
        call('TOPE', TOPE(encoding=3, text=track.original_artist)),
        call('TPE3', TPE3(encoding=3, text=track.original_title))
    ]

    mock_id3_instance.__setitem__.assert_has_calls(expected_setitem_calls, any_order=True)
    mock_id3_instance.save.assert_called_once()

@pytest.mark.asyncio
async def test_save_file_metadata_clear_tags(mock_id3_tags):
    # Arrange
    track = TrackDetails("/fake/path/file1.mp3", TrackManager())
    track.title = None
    track.artist = None
    track.album = None
    track.album_artist = None
    track.grouping = None
    track.original_album = None
    track.original_artist = None
    track.original_title = None
    track.id3 = id3.ID3(track.file_path)

    mock_id3_instance = mock_id3_tags({
        'TIT2': TIT2(encoding=3, text="Old Title"),
        'TPE1': TPE1(encoding=3, text="Old Artist"),
        'TALB': TALB(encoding=3, text="Old Album"),
        'TPE2': TPE2(encoding=3, text="Old Album Artist"),
        'TIT1': TIT1(encoding=3, text="Old Grouping"),
        'TOAL': TOAL(encoding=3, text="Old Original Album"),
        'TOPE': TOPE(encoding=3, text="Old Original Artist"),
        'TPE3': TPE3(encoding=3, text="Old Original Title"),
    })

    # Act
    track.save_file_metadata()

    # Assert
    expected_pop_calls = [
        call('TIT2', None),
        call('TPE1', None),
        call('TALB', None),
        call('TPE2', None),
        call('TIT1', None),
        call('TOAL', None),
        call('TOPE', None),
        call('TPE3', None)
    ]

    mock_id3_instance.pop.assert_has_calls(expected_pop_calls, any_order=True)
    mock_id3_instance.save.assert_not_called()

@pytest.mark.asyncio
async def test_save_file_metadata_partial_changes(mock_id3_tags):
    # Arrange
    track = TrackDetails("/fake/path/file1.mp3", TrackManager())
    track.title = "New Title"
    track.artist = "Old Artist"
    track.album = "New Album"
    track.album_artist = "Old Album Artist"
    track.grouping = "New Grouping"
    track.original_album = None
    track.original_artist = "Old Original Artist"
    track.original_title = None
    track.id3 = id3.ID3(track.file_path)

    mock_id3_instance = mock_id3_tags({
        'TIT2': TIT2(encoding=3, text="Old Title"),
        'TPE1': TPE1(encoding=3, text="Old Artist"),
        'TALB': TALB(encoding=3, text="Old Album"),
        'TPE2': TPE2(encoding=3, text="Old Album Artist"),
        'TIT1': TIT1(encoding=3, text="Old Grouping"),
        'TOAL': TOAL(encoding=3, text="Old Original Album"),
        'TOPE': TOPE(encoding=3, text="Old Original Artist"),
        'TPE3': TPE3(encoding=3, text="Old Original Title"),
    })

    # Act
    track.save_file_metadata()

    # Assert
    expected_setitem_calls = [
        call('TIT2', TIT2(encoding=3, text=track.title)),
        call('TALB', TALB(encoding=3, text=track.album)),
        call('TIT1', TIT1(encoding=3, text=track.grouping))
    ]
    expected_pop_calls = [
        call('TOAL', None),
        call('TPE3', None)
    ]

    mock_id3_instance.__setitem__.assert_has_calls(expected_setitem_calls, any_order=True)
    mock_id3_instance.pop.assert_has_calls(expected_pop_calls, any_order=True)
    mock_id3_instance.save.assert_called_once()

@pytest.mark.asyncio
async def test_get_formatted_artist():
    # Test case where custom_name is not None or empty
    artist = MbArtistDetails(
        name="Original Artist",
        type="Person",
        disambiguation="",
        sort_name="Original Artist",
        id="mock-id-1",
        aliases=[],
        type_id="type-id-1",
        joinphrase=""
    )
    artist.custom_name = "Custom Artist"
    assert artist.get_formatted_artist() == "Custom Artist", "Failed when custom_name is set"

    # Test case where custom_name is None
    artist.custom_name = None
    assert artist.get_formatted_artist() == "Original Artist", "Failed when custom_name is None"

    # Test case where custom_name is empty
    artist.custom_name = ""
    assert artist.get_formatted_artist() == "Original Artist", "Failed when custom_name is empty"

    # Test case where type is "character"
    artist.type = "character"
    artist.custom_name = "Custom Character"
    assert artist.get_formatted_artist() == "(Custom Character)", "Failed when type is 'character'"

    # Test case where type is "group"
    artist.type = "group"
    artist.custom_name = "Custom Group"
    assert artist.get_formatted_artist() == "(Custom Group)", "Failed when type is 'group'"

@pytest.mark.asyncio
async def test_get_artist_string():
    # Arrange
    manager = TrackManager()
    track = TrackDetails("/fake/path/file1.mp3", manager)

    artist1 = MbArtistDetails(
        name="Artist1",
        type="Person",
        disambiguation="",
        sort_name="Artist1, Firstname",
        id="mock-artist1-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase=""
    )
    artist1.custom_name = "Custom Artist1"

    artist2 = MbArtistDetails(
        name="Artist2",
        type="character",
        disambiguation="",
        sort_name="Artist2, Firstname",
        id="mock-artist2-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase=""
    )
    artist2.custom_name = "Custom Character2"

    track.mbArtistDetails = [artist1, artist2]

    # Act
    concatenated_string = track.get_artist_string()

    # Assert
    assert concatenated_string == "Custom Artist1; (Custom Character2)", "Failed to concatenate artist details correctly"

@pytest.mark.asyncio
async def test_gget_artist_string_empty():
    # Arrange
    manager = TrackManager()
    track = TrackDetails("/fake/path/file2.mp3", manager)
    track.mbArtistDetails = []

    # Act
    concatenated_string = track.get_artist_string()

    # Assert
    assert concatenated_string == "", "Failed to handle empty artist details list correctly"