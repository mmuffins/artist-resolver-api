import pytest
import httpx
from unittest.mock import AsyncMock

from TrackManager import TrackManager, MbArtistDetails

@pytest.fixture
def manager():
  return TrackManager()

@pytest.mark.asyncio
async def test_get_mbartist_customization_success(manager, mocker):
  # Make sure that 'manager' is an instance of TrackManager and not a coroutine
  assert isinstance(manager, TrackManager)  # This line is just for debugging

  # Create a mock for the AsyncClient.get method
  mock_get = AsyncMock(return_value=httpx.Response(status_code=200, json={
    'id': 1,
    'mbId': 'd6d7e9a4-df91-4bd7-ae96-f56bc98f0db1',
    'name': 'Chika Takami',
    'originalName': '高海千歌',
    'include': True
  }))
  mocker.patch('httpx.AsyncClient.get', mock_get)
  
  # Test the function
  response = await manager.get_mbartist_customization('d6d7e9a4-df91-4bd7-ae96-f56bc98f0db1')
  assert response is not None
  assert response['mbId'] == 'd6d7e9a4-df91-4bd7-ae96-f56bc98f0db1'
  assert response['name'] == 'Chika Takami'
  assert mock_get.called
  assert isinstance(manager, TrackManager)
  # Create a mock for the AsyncClient.get method
  mock_get = AsyncMock(return_value=httpx.Response(status_code=200, json={'id': 1, 'mbId': 'd6d7e9a4-df91-4bd7-ae96-f56bc98f0db1', 'name': 'Chika Takami', 'originalName': '高海千歌', 'include': True}))
  mocker.patch('httpx.AsyncClient.get', mock_get)
  
  # Test the function
  response = await manager.get_mbartist_customization('d6d7e9a4-df91-4bd7-ae96-f56bc98f0db1')
  assert response is not None
  assert response['mbId'] == 'd6d7e9a4-df91-4bd7-ae96-f56bc98f0db1'
  assert response['name'] == 'Chika Takami'
  assert mock_get.called

@pytest.mark.asyncio
async def test_get_mbartist_customization_not_found(manager, mocker):
  # Create a mock for the AsyncClient.get method that simulates a 404 error
  mock_get = AsyncMock(return_value=httpx.Response(status_code=404))
  mocker.patch('httpx.AsyncClient.get', mock_get)
  
  # Test the function
  response = await manager.get_mbartist_customization('eef388ad9-cd14-4cee-8fa0-invalid')
  assert response is None  # Depending on your implementation, you might check the handling of this case
  assert mock_get.called

@pytest.mark.asyncio
async def test_get_mbartist_customization_connection_error(manager, mocker):
  # Create a mock for the AsyncClient.get method that simulates a connection error
  mocker.patch('httpx.AsyncClient.get', side_effect=httpx.ConnectError)
  
  # Test the function
  with pytest.raises(httpx.ConnectError):
    await manager.get_mbartist_customization('d6d7e9a4-df91-4bd7-ae96-f56bc98f0db1')