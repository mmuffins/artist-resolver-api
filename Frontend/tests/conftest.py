import hashlib
import pytest
import httpx
import respx
import json
from unittest.mock import AsyncMock, patch, MagicMock
from TrackManager import TrackManager, MbArtistDetails, SimpleArtistDetails, TrackManager, TrackDetails

api_port = 23409
api_host = "localhost"


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
