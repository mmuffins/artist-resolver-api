import pytest
import httpx
import respx
import json
import asyncio
from unittest.mock import AsyncMock, patch, MagicMock
from TrackManager import TrackManager, MbArtistDetails, TrackManager, TrackDetails

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
def reference_track():
    """
    Returns a track details object with default values
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
async def test_load_mbartist_track_file(respx_mock, mock_id3_instance, reference_track):
    # Arrange1
    reference_track.product = None

    # individual id3 calls
    def id3_get_side_effect(tag, default):
        responses = {
            "TIT2": [reference_track.title],
            "TPE1": [reference_track.artist],
            "TALB": [reference_track.album],
            "TPE2": [reference_track.album_artist],
            "TIT1": [reference_track.grouping],
            "TOAL": [reference_track.original_album],
            "TOPE": [reference_track.original_artist],
            "TPE3": [reference_track.original_title],
        }
        return responses.get(tag, default)
    mock_id3_instance.get.side_effect = id3_get_side_effect
    
    mbid = "mock-93fb-4bc3-8ff9-065c75c4f90a"
    # id3 call for id3.getall("TXXX")
    mock_artist_relations = create_mock_txxx(
        description='artist_relations_json',
        text=[json.dumps([{
            "name": "あえ木八",
            "type": "Person",
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
async def test_load_simple_artist_track_file(respx_mock, mock_id3_instance, reference_track):
    # Arrange
    reference_track.product = "_"

    # individual id3 calls
    def id3_get_side_effect(tag, default):
        responses = {
            "TIT2": [reference_track.title],
            "TPE1": [reference_track.artist],
            "TALB": [reference_track.album],
            "TPE2": [reference_track.album_artist],
            "TIT1": [reference_track.grouping],
            "TOAL": [reference_track.original_album],
            "TOPE": [reference_track.original_artist],
            "TPE3": [reference_track.original_title],
        }
        return responses.get(tag, default)
    mock_id3_instance.get.side_effect = id3_get_side_effect
    
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

