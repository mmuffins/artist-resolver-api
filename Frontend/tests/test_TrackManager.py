import hashlib
import pytest
import httpx
import respx
import json
from unittest.mock import AsyncMock, patch, MagicMock
from TrackManager import TrackManager, MbArtistDetails, SimpleArtistDetails, TrackManager, TrackDetails

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
@respx.mock(assert_all_mocked=True)
async def test_split_artist_string_into_simple_artist_objects(respx_mock):
    # Arrange
    manager = TrackManager()
    track = TrackDetails("/fake/path/file1.mp3", manager)
    track.artist = "Artist1, Artist2 feat. Artist3 & Artist4, Character1 (CV: Artist5)"
    track.album_artist = "Various Artists"
    track.product = None

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

    expected_simple_artists = [
        {'name': "Artist1", 'type': "Person"},
        {'name': "Artist2", 'type': "Person"},
        {'name': "Artist3", 'type': "Person"},
        {'name': "Artist4", 'type': "Person"},
        {'name': "Artist5", 'type': "Person"},
        {'name': "Character1", 'type': "Character"}
    ]

    # Act
    await track.create_artist_objects()
    simple_artists = track.mbArtistDetails

    # Assert
    assert len(manager.artist_data) == 6, f"Unexpected number of entries in artist_data"
    assert len(simple_artists) == len(expected_simple_artists), f"Expected {len(expected_simple_artists)} simple artists, got {len(simple_artists)}"
    for i, artist in enumerate(simple_artists):
        assert artist.name == expected_simple_artists[i]['name'], f"Name mismatch at index {i}: expected {expected_simple_artists[i]['name']}, got {artist.name}"
        assert artist.type == expected_simple_artists[i]['type'], f"Type mismatch at index {i}: expected {expected_simple_artists[i]['type']}, got {artist.type}"

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_create_mbartist_objects_without_db_information(respx_mock):
    # Arrange
    manager = TrackManager()

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

    artist2 = MbArtistDetails(
        name="Artist2",
        type="Person",
        disambiguation="",
        sort_name="Artist2, Firstname",
        id="mock-artist2-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase=""
    )

    # Populate artist_data with MbArtistDetails
    manager.artist_data[artist1.mbid] = artist1
    manager.artist_data[artist2.mbid] = artist2

    # Mock the DB call to always return 404
    respx_mock.route(
        method="GET", 
        port__in=[api_port], 
        host=api_host, 
        path__regex=r"/api/mbartist/mbid/.*"
    ).mock(return_value=httpx.Response(404))

    # Add a catch-all for everything that's not explicitly routed
    # respx_mock.route().respond(404)

    # Act
    await manager.update_artists_info_from_db()

    # Assert
    assert manager.artist_data[artist1.mbid].custom_name == artist1.sort_name
    assert manager.artist_data[artist2.mbid].custom_name == artist2.sort_name
    assert manager.artist_data[artist1.mbid].custom_original_name == artist1.name
    assert manager.artist_data[artist2.mbid].custom_original_name == artist2.name
    assert manager.artist_data[artist1.mbid].include == artist1.include
    assert manager.artist_data[artist2.mbid].include == artist2.include

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_create_mbartist_objects_with_db_information(respx_mock):
    # Arrange
    manager = TrackManager()

    artist1 = MbArtistDetails(
        name="Artist1 Lastname",
        type="Person",
        disambiguation="",
        sort_name="Lastname, Artist1",
        id="mock-artist1-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase=""
    )

    artist1_expected = {
        'id': 239,
        'mbid': 'mock-artist1-id',
        'custom_name': 'Expected Lastname Artist1',
        'custom_original_name': 'Expected Lastname Artist1 Original',
        'include': False
    }

    # Populate artist_data with MbArtistDetails
    manager.artist_data[artist1.mbid] = artist1

    # Mock the DB call to return 200 for the specified mbid
    respx_mock.route(
        method="GET", 
        port__in=[api_port], 
        host=api_host, 
        path=f"/api/mbartist/mbid/mock-artist1-id"
    ).mock(return_value=httpx.Response(
        200, json={
            'id': artist1_expected["id"],
            'mbid': artist1_expected["mbid"],
            'name': artist1_expected["custom_name"],
            'originalName': artist1_expected["custom_original_name"],
            'include': artist1_expected["include"]
        }
    ))

    # Act
    await manager.update_artists_info_from_db()

    # Assert
    assert manager.artist_data[artist1.mbid].id == artist1_expected["id"]
    assert manager.artist_data[artist1.mbid].mbid == artist1_expected["mbid"]
    assert manager.artist_data[artist1.mbid].custom_name == artist1_expected["custom_name"]
    assert manager.artist_data[artist1.mbid].custom_original_name == artist1_expected["custom_original_name"]
    assert manager.artist_data[artist1.mbid].include == artist1_expected["include"]

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_create_simple_artist_objects_with_unknown_alias(respx_mock):
    # Arrange
    manager = TrackManager()

    artist1 = SimpleArtistDetails(
        name="SimpleArtist1",
        type="Person",
        disambiguation="",
        sort_name="SimpleArtist1",
        id="mock-artist1-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="TestProduct",
        product_id="1"
    )

    artist2 = SimpleArtistDetails(
        name="SimpleArtist2",
        type="Person",
        disambiguation="",
        sort_name="SimpleArtist2",
        id="mock-artist2-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="TestProduct",
        product_id="2"
    )

    # Populate artist_data with SimpleArtistDetails
    manager.artist_data[artist1.mbid] = artist1
    manager.artist_data[artist2.mbid] = artist2

    # Mock the DB call to return an empty object
    respx_mock.route(
        method="GET", 
        port__in=[api_port], 
        host=api_host, 
        path__regex=r"/api/alias.*"
    ).mock(return_value=httpx.Response(200, text="[]"))

    # Act
    await manager.update_artists_info_from_db()

    # Assert
    assert manager.artist_data[artist1.mbid].custom_name == artist1.sort_name
    assert manager.artist_data[artist2.mbid].custom_name == artist2.sort_name
    assert manager.artist_data[artist1.mbid].custom_original_name == artist1.name
    assert manager.artist_data[artist2.mbid].custom_original_name == artist2.name
    assert manager.artist_data[artist1.mbid].include == artist1.include
    assert manager.artist_data[artist2.mbid].include == artist2.include

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_create_simple_artist_objects_with_db_information(respx_mock):
    # Arrange
    manager = TrackManager()

    artist1 = SimpleArtistDetails(
        name="SimpleArtist1",
        type="Person",
        disambiguation="",
        sort_name="SimpleArtist1",
        id="mock-artist1-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="TestProduct",
        product_id="1"
    )

    artist1_expected = {
        'id': 248,
        'name': 'SimpleArtist1',
        'artistId': 41,
        'artist': 'CustomSimpleArtist1',
        'franchiseId': 1,
        'franchise': '_'
    }

    artist2 = SimpleArtistDetails(
        name="SimpleArtist2",
        type="Person",
        disambiguation="",
        sort_name="SimpleArtist2",
        id="mock-artist2-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="_",
        product_id="1"
    )

    artist2_expected = {
        'id': 249,
        'name': 'SimpleArtist2',
        'artistId': 42,
        'artist': 'CustomSimpleArtist2',
        'franchiseId': 1,
        'franchise': '_'
    }

    # Populate artist_data with SimpleArtistDetails
    manager.artist_data[artist1.mbid] = artist1
    manager.artist_data[artist2.mbid] = artist2

    # Mock the DB call to return 200 for the specified artist names with franchiseId

    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/alias",
        params={"name": "SimpleArtist1", "franchiseId": "1"}
    ).mock(return_value=httpx.Response(200, json=[artist1_expected]))

    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/alias",
        params={"name": "SimpleArtist2", "franchiseId": "1"}
    ).mock(return_value=httpx.Response(200, json=[artist2_expected]))

    # Act
    await manager.update_artists_info_from_db()

    # Assert
    assert manager.artist_data[artist1.mbid].custom_name == artist1_expected['artist'], f"Expected {artist1_expected['artist']}, got {manager.artist_data[artist1.mbid].custom_name}"
    assert manager.artist_data[artist1.mbid].custom_original_name == artist1_expected['name'], f"Expected {artist1_expected['name']}, got {manager.artist_data[artist1.mbid].custom_original_name}"
    assert manager.artist_data[artist1.mbid].id == artist1_expected['artistId'], f"Expected {artist1_expected['artistId']}, got {manager.artist_data[artist1.mbid].id}"
    assert manager.artist_data[artist1.mbid].include == artist1.include

    assert manager.artist_data[artist2.mbid].custom_name == artist2_expected['artist'], f"Expected {artist2_expected['artist']}, got {manager.artist_data[artist2.mbid].custom_name}"
    assert manager.artist_data[artist2.mbid].custom_original_name == artist2_expected['name'], f"Expected {artist2_expected['name']}, got {manager.artist_data[artist2.mbid].custom_original_name}"
    assert manager.artist_data[artist2.mbid].id == artist2_expected['artistId'], f"Expected {artist2_expected['artistId']}, got {manager.artist_data[artist2.mbid].id}"
    assert manager.artist_data[artist2.mbid].include == artist2.include


@pytest.mark.asyncio
async def test_parse_simple_artist_franchise():
    # Arrange
    product_list = [
        {"id": 1, "name": "_"},
        {"id": 2, "name": "Franchise1"},
        {"id": 3, "name": "Franchise2"},
    ]

    # Act & Assert
    result = SimpleArtistDetails.parse_simple_artist_franchise(None, product_list[1]["name"], product_list)
    assert result == product_list[1]

    result = SimpleArtistDetails.parse_simple_artist_franchise(product_list[2]["name"], None, product_list)
    assert result == product_list[2]

    result = SimpleArtistDetails.parse_simple_artist_franchise(product_list[2]["name"], product_list[1]["name"], product_list)
    assert result == product_list[2]

    result = SimpleArtistDetails.parse_simple_artist_franchise(None, "NonExistentFranchise", product_list)
    assert result == product_list[0]

    result = SimpleArtistDetails.parse_simple_artist_franchise(None, None, product_list)
    assert result == product_list[0]

def test_simple_artist_generate_instance_hash():
    # Arrange
    artist = SimpleArtistDetails(
        name="ArtistName",
        type="Person",
        disambiguation="",
        sort_name="ArtistName",
        id="mock-artist-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="Product",
        product_id="1"
    )
    unique_string = "ArtistName-1"

    # Act
    result = artist.generate_instance_hash(unique_string)

    # Assert
    expected_hash = hashlib.sha256(unique_string.encode()).hexdigest()
    assert result == expected_hash, f"Expected hash {expected_hash}, got {result}"

def test_simple_artist_split_artist():
    # Arrange
    artist_string = "Artist1, Artist2 feat. Artist3 & Artist4, Character1 (CV: Artist5)"
    expected_result = [
        {"type": "Person", "include": True, "name": "Artist1"},
        {"type": "Person", "include": True, "name": "Artist2"},
        {"type": "Person", "include": True, "name": "Artist3"},
        {"type": "Person", "include": True, "name": "Artist4"},
        {"type": "Person", "include": True, "name": "Artist5"},
        {"type": "Character", "include": False, "name": "Character1"},
    ]

    # Act
    result = SimpleArtistDetails.split_artist(artist_string)

    # Assert
    assert len(result) == len(expected_result), f"Expected {len(expected_result)} artists, got {len(result)}"
    for i in range(len(result)):
        assert result[i]["name"] == expected_result[i]["name"], f"Name mismatch at index {i}: expected {expected_result[i]['name']}, got {result[i]['name']}"
        assert result[i]["type"] == expected_result[i]["type"], f"Type mismatch at index {i}: expected {expected_result[i]['type']}, got {result[i]['type']}"
        assert result[i]["include"] == expected_result[i]["include"], f"Include mismatch at index {i}: expected {expected_result[i]['include']}, got {result[i]['include']}"


@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_update_db_simple_artist_alias_and_artist_do_not_exist_in_db(respx_mock):
    # Arrange
    manager = TrackManager()

    server_artist_id = 99
    server_alias_id = 88

    artist = SimpleArtistDetails(
        name="NewSimpleArtist",
        type="Person",
        disambiguation="",
        sort_name="NewSimpleArtist",
        id="-1",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="TestProduct",
        product_id="1"
    )

    artist.custom_name = "NewCustomName"
    manager.artist_data[artist.mbid] = artist

    # Mock the GET requests to simulate checking if the artist and alias exist
    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/artist"
    ).mock(return_value=httpx.Response(200, text="[]"))

    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/alias"
    ).mock(return_value=httpx.Response(200, text="[]"))

    # Mock the POST requests to create new artist and alias
    respx_mock.route(
        method="POST",
        port=api_port,
        host=api_host,
        path="/api/artist",
    ).mock(return_value=httpx.Response(200, json={"id":server_artist_id,"name":artist.custom_name,"aliases":[]}))

    
    respx_mock.route(
        method="POST",
        port=api_port,
        host=api_host,
        path="/api/alias"
    ).mock(return_value=httpx.Response(200, json={"id":server_alias_id,"name":artist.name,"artistId":server_artist_id,"artist":artist.custom_name,"franchiseId":1,"franchise":"_"}))

    # Act
    await manager.send_changes_to_db()

    # Assert
    assert respx_mock.calls.call_count == 4, "Expected four calls: two GET requests and two POST requests"
    
    # verify if artist exists
    assert respx_mock.calls[0].request.method == "GET", "Call to verify if an artist exist was not of type GET"

    # post new artist
    post_new_artist_call = respx_mock.calls[1].request 
    assert post_new_artist_call.method == "POST", "Call to create new artist was not of type POST"
    post_new_artist_call_content = json.loads(post_new_artist_call.content.decode())
    assert post_new_artist_call_content == {'Name': artist.custom_name}, f"Post body to create new artist did not match expected object"
    
    # verify if alias exists
    assert respx_mock.calls[2].request.method == "GET", "Call to verify if an alias exist was not of type GET"

    # post new alias
    post_new_alias_call = respx_mock.calls[3].request 
    assert post_new_alias_call.method == "POST", "Call to create new alias was not of type POST"
    post_new_alias_call_content = json.loads(post_new_alias_call.content.decode())
    assert post_new_alias_call_content == {'Name': artist.name, 'artistid': server_artist_id, 'franchiseid': '1'}, f"Post body to create new alias did not match expected object"


@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_update_db_simple_artist_all_equal_to_db(respx_mock):
    # Arrange
    manager = TrackManager()

    server_artist_id = 99
    server_alias_id = 88

    artist = SimpleArtistDetails(
        name="NewSimpleArtist",
        type="Person",
        disambiguation="",
        sort_name="NewSimpleArtist",
        id="-1",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="TestProduct",
        product_id=1
    )

    artist.custom_name = "NewCustomName"

    manager.artist_data[artist.mbid] = artist

    # Mock the GET requests to simulate checking if the artist and alias exist
    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/artist"
    ).mock(return_value=httpx.Response(200, json=[{
        'id': server_artist_id,
        'name': artist.custom_name,
    }]))

    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/alias",
        params={"name": artist.name, "franchiseId": artist.product_id}
    ).mock(return_value=httpx.Response(200, json=[{
        'id': server_alias_id,
        'name': artist.name,
        'artistId': server_artist_id,
        'artist': artist.custom_name,
        'franchiseId': artist.product_id,
        'franchise': artist.product
    }]))

    # Act
    await manager.send_changes_to_db()

    # Assert
    assert respx_mock.calls.call_count == 2, "Expected only two calls to check if artist and alias exist."

    # verify if artist exists
    assert respx_mock.calls[0].request.method == "GET", "Call to verify if an artist exist was not of type GET"
    assert respx_mock.calls[0].request.url.params["name"] == artist.custom_name, "Call to verify if an artist exist used an unexpected parameter"
    
    # verify if alias exists
    assert respx_mock.calls[1].request.method == "GET", "Call to verify if an alias exist was not of type GET"
    assert respx_mock.calls[1].request.url.params["name"] == artist.name, "Call to verify if an artist exist used an unexpected parameter"
    assert respx_mock.calls[1].request.url.params["franchiseId"] == str(artist.product_id), "Call to verify if an artist exist used an unexpected parameter"
    

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_update_db_simple_artist_new_alias_existing_artist_in_db(respx_mock):
    # Arrange
    manager = TrackManager()

    server_artist_id = 99
    server_alias_id = 88

    artist = SimpleArtistDetails(
        name="NewSimpleArtist",
        type="Person",
        disambiguation="",
        sort_name="NewSimpleArtist",
        id="-1",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="TestProduct",
        product_id="1"
    )

    artist.custom_name = "NewCustomName"
    manager.artist_data[artist.mbid] = artist

    # Mock the GET requests to simulate that the artist exists, but alias doesn't
    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/alias"
    ).mock(return_value=httpx.Response(200, text="[]"))

    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/artist"
    ).mock(return_value=httpx.Response(200, json=[{
        'id': server_artist_id,
        'name': artist.custom_name,
    }]))

    respx_mock.route(
        method="POST",
        port=api_port,
        host=api_host,
        path="/api/alias"
    ).mock(return_value=httpx.Response(200, json={"id":server_alias_id,"name":artist.name,"artistId":server_artist_id,"artist":artist.custom_name,"franchiseId":1,"franchise":"_"}))

    # Act
    await manager.send_changes_to_db()

    # Assert
    assert respx_mock.calls.call_count == 3, "Expected only two calls to check if artist and alias exist."

    # verify if artist exists
    assert respx_mock.calls[0].request.method == "GET", "Call to verify if an artist exist was not of type GET"
    assert respx_mock.calls[0].request.url.params["name"] == artist.custom_name, "Call to verify if an artist exist used an unexpected parameter"
    
    # verify if alias exists
    assert respx_mock.calls[1].request.method == "GET", "Call to verify if an alias exist was not of type GET"
    assert respx_mock.calls[1].request.url.params["name"] == artist.name, "Call to verify if an artist exist used an unexpected parameter"
    assert respx_mock.calls[1].request.url.params["franchiseId"] == str(artist.product_id), "Call to verify if an artist exist used an unexpected parameter"

    # post new alias
    post_new_alias_call = respx_mock.calls[2].request 
    assert post_new_alias_call.method == "POST", "Call to create new alias was not of type POST"
    post_new_alias_call_content = json.loads(post_new_alias_call.content.decode())
    assert post_new_alias_call_content == {'Name': artist.name, 'artistid': server_artist_id, 'franchiseid': '1'}, f"Post body to create new alias did not match expected object"



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

























    

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_send_changes_to_db_with_updated_artist(respx_mock):
    # Arrange
    manager = TrackManager()

    artist = SimpleArtistDetails(
        name="UpdatedSimpleArtist",
        type="Person",
        disambiguation="",
        sort_name="UpdatedSimpleArtist",
        id="mock-artist-id",
        aliases=[],
        type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
        joinphrase="",
        product="TestProduct",
        product_id="1"
    )

    artist.id = 201  # Existing artist ID in the database
    manager.artist_data[artist.mbid] = artist

    # Mock the GET requests to simulate checking if the artist and alias exist
    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/artist"
    ).mock(return_value=httpx.Response(200, json={
        'id': artist.id,
        'name': artist.custom_name
    }))

    respx_mock.route(
        method="GET",
        port=api_port,
        host=api_host,
        path="/api/alias"
    ).mock(return_value=httpx.Response(200, json=[]))

    # Mock the PUT request to update the existing artist
    respx_mock.route(
        method="PUT",
        port=api_port,
        host=api_host,
        path=f"/api/artist/id/{artist.id}"
    ).mock(return_value=httpx.Response(200))

    # Mock the POST request to create new alias
    respx_mock.route(
        method="POST",
        port=api_port,
        host=api_host,
        path="/api/alias"
    ).mock(return_value=httpx.Response(200))

    # Act
    await manager.send_changes_to_db()

    # Assert
    assert respx_mock.calls.call_count == 4, "Expected four calls: two GET requests, one PUT request, and one POST request"
    assert respx_mock.calls[0].request.method == "GET"
    assert respx_mock.calls[1].request.method == "GET"
    assert respx_mock.calls[2].request.method == "PUT"
    assert respx_mock.calls[3].request.method == "POST"