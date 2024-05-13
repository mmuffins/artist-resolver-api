import pytest
import httpx
import respx
import json
from unittest.mock import AsyncMock, patch, MagicMock
from TrackManager import TrackManager, MbArtistDetails, TrackManager, TrackDetails

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



@pytest.fixture
def mock_id3_instance(mocker):
    """
    Fixture that returns a mocked mutagen id3 instance
    """

    mocked_id3 = mocker.patch('mutagen.id3.ID3', autospec=True)
    mock_id3_instance = MagicMock()
    mocked_id3.return_value = mock_id3_instance
    # Mock the main get_id3_object call to get a dummy object
    mocker.patch('TrackManager.TrackDetails.get_id3_object', return_value=mock_id3_instance)
    return mock_id3_instance

@pytest.fixture
def mock_id3_tags(mock_id3_instance):
    """
    A fixture to configure mock ID3 tags on a provided mock ID3 instance.
    """

    def apply_mock(tags):
        def id3_get_side_effect(tag, default):
            return tags.get(tag, default)
        mock_id3_instance.get.side_effect = id3_get_side_effect
        return mock_id3_instance
    return apply_mock

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

    # Mocking the 404 response for a specific MBID
    # respx_mock.get(f"/mbartist/mbid/{mbid}").mock(
    #     return_value=httpx.Response(200, json={'id': 1, 'mbId': 'f3688ad9-cd14-4cee-8fa0-0f4434e762bb', 'name': 'ClariS-Changed', 'originalName': 'ClariS-Original-Changed', 'include': True})
    # )
    # respx_mock.route(
    #     method="GET", 
    #     port__in=[23409], 
    #     host="localhost", 
    #     path=f"/api/mbartist/mbid/{mbid}"
    # ).mock(return_value=httpx.Response(404))

    # Add a catch-all for everything that's not explicitly routed
    # respx_mock.route().respond(404)
    
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
        port__in=[23409], 
        host="localhost", 
        path="/api/franchise"
    ).mock(return_value=httpx.Response(
        200, json=[
            {'id': 1, 'name': '_', 'aliases': []}, 
            {'id': 2, 'name': 'TestFranchise1', 'aliases': []}, 
            {'id': 3, 'name': 'TestFranchise2', 'aliases': []}, 
            {'id': 4, 'name': 'TestFranchise3', 'aliases': []}
        ]
    ))

    # Add a catch-all for everything that's not explicitly routed
    # respx_mock.route().respond(504, text="Route was not mocked")
    
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
async def test_parse_artist_json_with_nested_objects(respx_mock):
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

    # Mocking the 404 response for a specific MBID
    # respx_mock.get(f"/mbartist/mbid/{mbid}").mock(
    #     return_value=httpx.Response(200, json={'id': 1, 'mbId': 'f3688ad9-cd14-4cee-8fa0-0f4434e762bb', 'name': 'ClariS-Changed', 'originalName': 'ClariS-Original-Changed', 'include': True})
    # )
    # respx_mock.route(
    #     method="GET", 
    #     port__in=[23409], 
    #     host="localhost", 
    #     path=f"/api/mbartist/mbid/{mbid}"
    # ).mock(return_value=httpx.Response(404))

    # Add a catch-all for everything that's not explicitly routed
    # respx_mock.route().respond(404)
    
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
        # assert artists[i].joinphrase == expected[i]["joinphrase"], f"joinphrase mismatch at index {i}: expected {expected[i]["joinphrase"]}, got {artists[i].joinphrase}"
        # assert artists[i].custom_name == expected[i]["custom_name"]
        # assert artists[i].custom_original_name == expected[i]["custom_original_name"]
        # assert artists[i].include == expected[i]["include"]


async def test_split_artist_string_into_simple_artist():
    pass

async def test_create_mbartist_objects_without_db_information():
    pass

async def test_create_mbartist_objects_with_db_information():
    pass

async def test_create_simple_artist_objects_with_db_information():
    pass

async def test_create_simple_artist_objects_without_db_information():
    pass

async def test_update_db_simple_artist_name_does_not_exist_in_db():
    # simple 1: artist and alias does not exist on server
    pass

async def test_update_db_simple_artist_unchanged_from_db():
    # simple 2: artist / alias exist on server and is equal to local data
    pass

async def test_update_db_simple_artist_name_was_changed_to_artist_already_in_db():
    # simple 4: artist / alias exist on server, custom name was changed to artist that already exists on server
    pass

async def test_update_db_simple_artist_name_was_changed_to_artist_not_already_in_db():
    # simple 3: artist / alias exist on server, custom name was changed to artist that doesn't exist on server
    pass

async def test_update_db_mbartist_artist_does_not_exist_in_db():
    # mbid 2: mbid already exists on server,is equal to local data
    pass

async def test_update_db_mbartist_unchanged_from_db():
    # mbid 1: mbid does not exist on server
    pass

async def test_update_db_mbartist_name_was_changed():
    # mbid 3: mbid already exists on server, custom name was changed
    pass
