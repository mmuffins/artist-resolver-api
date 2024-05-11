import pytest
import httpx
import asyncio
from unittest.mock import AsyncMock, patch, MagicMock
from TrackManager import TrackManager, MbArtistDetails, TrackManager, TrackDetails

# @pytest.fixture
# def manager():
#     return TrackManager()

def create_mock_txxx(description, text):
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
async def test_trackdetails_read_file_metadata(mocker):
    # Arrange
    # Mock os.walk to simulate directory traversal
    test_directory = "/fake/dir"
    mocker.patch('os.walk', return_value=[
        (test_directory, (), ['file1.mp3']),
    ])

    # Mock ID3 to simulate metadata extraction
    mocked_id3 = mocker.patch('mutagen.id3.ID3', autospec=True)
    mock_id3_instance = MagicMock()
    mocked_id3.return_value = mock_id3_instance

    # individual id3 calls

    def id3_get_side_effect(tag, default):
        responses = {
            "TIT2": ["Test Title"],
            "TPE1": ["Test Artist"],
            "TALB": ["album"],
            "TPE2": ["album_artist"],
            "TIT1": ["grouping"],
            "TOAL": ["original_album"],
            "TOPE": ["original_artist"],
            "TPE3": ["original_title"],
        }
        return responses.get(tag, default)
    mock_id3_instance.get.side_effect = id3_get_side_effect
    
    # id3 call for id3.getall("TXXX")
    mock_artist_relations = create_mock_txxx(
      description='artist_relations_json',
      text=['[{"name": "ClariS", "type": "Group", "disambiguation": "J-Pop", "sort_name": "ClariS", "id": "f3688ad9-cd14-4cee-8fa0-0f4434e762bb", "aliases": [{"sort-name": "クラリス", "begin": "2010-09-10", "name": "ClariS", "end": null, "primary": true, "type": "Artist name", "locale": "ja", "ended": false, "type-id": "894afba6-2816-3c24-8072-eadb66bd04bc"}, {"locale": null, "type": "Search hint", "type-id": "1937e404-b981-3cb7-8151-4c86ebfc8d8e", "ended": false, "name": "アリス★クララ", "sort-name": "アリス★クララ", "begin": null, "end": null, "primary": null}, {"begin": "2009-10-10", "sort-name": "アリス☆クララ", "name": "アリス☆クララ", "primary": false, "end": "2010-09-10", "type": "Artist name", "locale": "ja", "ended": true, "type-id": "894afba6-2816-3c24-8072-eadb66bd04bc"}], "type_id": "e431f5f6-b5d2-343d-8b36-72607fffb74b", "relations": [{"name": "アリス", "type": "Person", "disambiguation": "ClariS", "sort_name": "Alice", "id": "9a542279-446a-457b-9a19-65a24e7da35b", "aliases": [], "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df", "relations": []}, {"name": "カレン", "type": "Person", "disambiguation": "ClariS", "sort_name": "Karen", "id": "97b0f78f-dbdb-435d-b0f6-b386b46c0c91", "aliases": [], "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df", "relations": []}, {"name": "アリス", "type": "Person", "disambiguation": "ClariS", "sort_name": "Alice", "id": "9a542279-446a-457b-9a19-65a24e7da35b", "aliases": [], "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df", "relations": []}, {"name": "クララ", "type": "Person", "disambiguation": "ClariS", "sort_name": "Clara", "id": "5c66d927-bb54-4de0-a498-175bfe84acf8", "aliases": [], "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df", "relations": []}, {"name": "クララ", "type": "Person", "disambiguation": "ClariS", "sort_name": "Clara", "id": "5c66d927-bb54-4de0-a498-175bfe84acf8", "aliases": [], "type_id": "b6e035f4-3ce9-331c-97df-83397230b0df", "relations": []}], "joinphrase": ""}]']
    )
    mock_id3_instance.getall.return_value = [mock_artist_relations] 

    # Mock the main read_id3_tags call to get a dummy object
    mocker.patch('TrackManager.TrackDetails.read_id3_tags', return_value=mock_id3_instance)



    # Act
    manager = TrackManager()
    # asyncio.run(manager.load_directory(test_directory))
    track = TrackDetails("/fake/path/file1.mp3", manager)
    await track.read_file_metadata()


    # Check that the tracks list is populated as expected
    assert len(manager.tracks) == 2
    assert manager.tracks[0].artist == "Test Artist"
    assert all(isinstance(track, TrackDetails) for track in manager.tracks)

    # Optionally, check if the correct metadata tags were accessed
    mock_id3_instance.get.assert_any_call('TPE1', [''])

# @pytest.mark.asyncio
# async def test_get_mbartist_customization_success(manager, mocker):
#   # Make sure that 'manager' is an instance of TrackManager and not a coroutine
#   assert isinstance(manager, TrackManager)  # This line is just for debugging

#   # Create a mock for the AsyncClient.get method
#   mock_get = AsyncMock(return_value=httpx.Response(status_code=200, json={
#     'id': 1,
#     'mbId': 'd6d7e9a4-df91-4bd7-ae96-f56bc98f0db1',
#     'name': 'Chika Takami',
#     'originalName': '高海千歌',
#     'include': True
#   }))
1#   mocker.patch('httpx.AsyncClient.get', mock_get)
  
#   # Test the function
#   response = await manager.get_mbartist_customization('d6d7e9a4-df91-4bd7-ae96-f56bc98f0db1')
#   assert response is not None
#   assert response['mbId'] == 'd6d7e9a4-df91-4bd7-ae96-f56bc98f0db1'
#   assert response['name'] == 'Chika Takami'
#   assert mock_get.called
#   assert isinstance(manager, TrackManager)
#   # Create a mock for the AsyncClient.get method
#   mock_get = AsyncMock(return_value=httpx.Response(status_code=200, json={'id': 1, 'mbId': 'd6d7e9a4-df91-4bd7-ae96-f56bc98f0db1', 'name': 'Chika Takami', 'originalName': '高海千歌', 'include': True}))
#   mocker.patch('httpx.AsyncClient.get', mock_get)
  
#   # Test the function
#   response = await manager.get_mbartist_customization('d6d7e9a4-df91-4bd7-ae96-f56bc98f0db1')
#   assert response is not None
#   assert response['mbId'] == 'd6d7e9a4-df91-4bd7-ae96-f56bc98f0db1'
#   assert response['name'] == 'Chika Takami'
#   assert mock_get.called

# @pytest.mark.asyncio
# async def test_get_mbartist_customization_not_found(manager, mocker):
#   # Create a mock for the AsyncClient.get method that simulates a 404 error
#   mock_get = AsyncMock(return_value=httpx.Response(status_code=404))
#   mocker.patch('httpx.AsyncClient.get', mock_get)
  
#   # Test the function
#   response = await manager.get_mbartist_customization('eef388ad9-cd14-4cee-8fa0-invalid')
#   assert response is None  # Depending on your implementation, you might check the handling of this case
#   assert mock_get.called

# @pytest.mark.asyncio
# async def test_get_mbartist_customization_connection_error(manager, mocker):
#   # Create a mock for the AsyncClient.get method that simulates a connection error
#   mocker.patch('httpx.AsyncClient.get', side_effect=httpx.ConnectError)
  
#   # Test the function
#   with pytest.raises(httpx.ConnectError):
#     await manager.get_mbartist_customization('d6d7e9a4-df91-4bd7-ae96-f56bc98f0db1')