
import hashlib
import pytest
import httpx
import respx
import json
from unittest.mock import AsyncMock, patch, MagicMock
from TrackManager import TrackManager, MbArtistDetails, SimpleArtistDetails, TrackManager, TrackDetails

api_port = 23409
api_host = "localhost"

@pytest.mark.asyncio
@respx.mock(assert_all_mocked=True)
async def test_create_artist_objects_with_unknown_alias(respx_mock):
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
async def test_create_artist_objects_with_db_information(respx_mock):
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
async def test_parse_franchise():
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

def test_generate_instance_hash():
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

def test_split_artist():
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
async def artist_without_id_not_found_when_saving(respx_mock):
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
    await TrackManager.send_simple_artist_changes_to_db(artist)

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
async def artist_with_id_found_by_id_when_saving(respx_mock):
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
async def test_update_db_new_alias_existing_artist_in_db(respx_mock):
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





# @pytest.mark.asyncio
# @respx.mock(assert_all_mocked=True)
# async def test_update_db_alias_and_artist_do_not_exist_in_db(respx_mock):
#     # Arrange
#     manager = TrackManager()

#     server_artist_id = 99
#     server_alias_id = 88

#     artist = SimpleArtistDetails(
#         name="NewSimpleArtist",
#         type="Person",
#         disambiguation="",
#         sort_name="NewSimpleArtist",
#         id="-1",
#         aliases=[],
#         type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
#         joinphrase="",
#         product="TestProduct",
#         product_id="1"
#     )

#     artist.custom_name = "NewCustomName"
#     manager.artist_data[artist.mbid] = artist

#     # Mock the GET requests to simulate checking if the artist and alias exist
#     respx_mock.route(
#         method="GET",
#         port=api_port,
#         host=api_host,
#         path="/api/artist"
#     ).mock(return_value=httpx.Response(200, text="[]"))

#     respx_mock.route(
#         method="GET",
#         port=api_port,
#         host=api_host,
#         path="/api/alias"
#     ).mock(return_value=httpx.Response(200, text="[]"))

#     # Mock the POST requests to create new artist and alias
#     respx_mock.route(
#         method="POST",
#         port=api_port,
#         host=api_host,
#         path="/api/artist",
#     ).mock(return_value=httpx.Response(200, json={"id":server_artist_id,"name":artist.custom_name,"aliases":[]}))

    
#     respx_mock.route(
#         method="POST",
#         port=api_port,
#         host=api_host,
#         path="/api/alias"
#     ).mock(return_value=httpx.Response(200, json={"id":server_alias_id,"name":artist.name,"artistId":server_artist_id,"artist":artist.custom_name,"franchiseId":1,"franchise":"_"}))

#     # Act
#     await TrackManager.send_simple_artist_changes_to_db(artist)

#     # Assert
#     assert respx_mock.calls.call_count == 4, "Expected four calls: two GET requests and two POST requests"
    
#     # verify if artist exists
#     assert respx_mock.calls[0].request.method == "GET", "Call to verify if an artist exist was not of type GET"

#     # post new artist
#     post_new_artist_call = respx_mock.calls[1].request 
#     assert post_new_artist_call.method == "POST", "Call to create new artist was not of type POST"
#     post_new_artist_call_content = json.loads(post_new_artist_call.content.decode())
#     assert post_new_artist_call_content == {'Name': artist.custom_name}, f"Post body to create new artist did not match expected object"
    
#     # verify if alias exists
#     assert respx_mock.calls[2].request.method == "GET", "Call to verify if an alias exist was not of type GET"

#     # post new alias
#     post_new_alias_call = respx_mock.calls[3].request 
#     assert post_new_alias_call.method == "POST", "Call to create new alias was not of type POST"
#     post_new_alias_call_content = json.loads(post_new_alias_call.content.decode())
#     assert post_new_alias_call_content == {'Name': artist.name, 'artistid': server_artist_id, 'franchiseid': '1'}, f"Post body to create new alias did not match expected object"


# @pytest.mark.asyncio
# @respx.mock(assert_all_mocked=True)
# async def artist_with_id_found_by_id_when_saving(respx_mock):
#     # Arrange
#     manager = TrackManager()

#     server_artist_id = 99
#     server_alias_id = 88

#     artist = SimpleArtistDetails(
#         name="NewSimpleArtist",
#         type="Person",
#         disambiguation="",
#         sort_name="NewSimpleArtist",
#         id="-1",
#         aliases=[],
#         type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
#         joinphrase="",
#         product="TestProduct",
#         product_id=1
#     )

#     artist.custom_name = "NewCustomName"

#     manager.artist_data[artist.mbid] = artist

#     # Mock the GET requests to simulate checking if the artist and alias exist
#     respx_mock.route(
#         method="GET",
#         port=api_port,
#         host=api_host,
#         path="/api/artist"
#     ).mock(return_value=httpx.Response(200, json=[{
#         'id': server_artist_id,
#         'name': artist.custom_name,
#     }]))

#     respx_mock.route(
#         method="GET",
#         port=api_port,
#         host=api_host,
#         path="/api/alias",
#         params={"name": artist.name, "franchiseId": artist.product_id}
#     ).mock(return_value=httpx.Response(200, json=[{
#         'id': server_alias_id,
#         'name': artist.name,
#         'artistId': server_artist_id,
#         'artist': artist.custom_name,
#         'franchiseId': artist.product_id,
#         'franchise': artist.product
#     }]))

#     # Act
#     await manager.send_changes_to_db()

#     # Assert
#     assert respx_mock.calls.call_count == 2, "Expected only two calls to check if artist and alias exist."

#     # verify if artist exists
#     assert respx_mock.calls[0].request.method == "GET", "Call to verify if an artist exist was not of type GET"
#     assert respx_mock.calls[0].request.url.params["name"] == artist.custom_name, "Call to verify if an artist exist used an unexpected parameter"
    
#     # verify if alias exists
#     assert respx_mock.calls[1].request.method == "GET", "Call to verify if an alias exist was not of type GET"
#     assert respx_mock.calls[1].request.url.params["name"] == artist.name, "Call to verify if an artist exist used an unexpected parameter"
#     assert respx_mock.calls[1].request.url.params["franchiseId"] == str(artist.product_id), "Call to verify if an artist exist used an unexpected parameter"
    

# @pytest.mark.asyncio
# @respx.mock(assert_all_mocked=True)
# async def test_update_db_new_alias_existing_artist_in_db(respx_mock):
#     # Arrange
#     manager = TrackManager()

#     server_artist_id = 99
#     server_alias_id = 88

#     artist = SimpleArtistDetails(
#         name="NewSimpleArtist",
#         type="Person",
#         disambiguation="",
#         sort_name="NewSimpleArtist",
#         id="-1",
#         aliases=[],
#         type_id="b6e035f4-3ce9-331c-97df-83397230b0df",
#         joinphrase="",
#         product="TestProduct",
#         product_id="1"
#     )

#     artist.custom_name = "NewCustomName"
#     manager.artist_data[artist.mbid] = artist

#     # Mock the GET requests to simulate that the artist exists, but alias doesn't
#     respx_mock.route(
#         method="GET",
#         port=api_port,
#         host=api_host,
#         path="/api/alias"
#     ).mock(return_value=httpx.Response(200, text="[]"))

#     respx_mock.route(
#         method="GET",
#         port=api_port,
#         host=api_host,
#         path="/api/artist"
#     ).mock(return_value=httpx.Response(200, json=[{
#         'id': server_artist_id,
#         'name': artist.custom_name,
#     }]))

#     respx_mock.route(
#         method="POST",
#         port=api_port,
#         host=api_host,
#         path="/api/alias"
#     ).mock(return_value=httpx.Response(200, json={"id":server_alias_id,"name":artist.name,"artistId":server_artist_id,"artist":artist.custom_name,"franchiseId":1,"franchise":"_"}))

#     # Act
#     await manager.send_changes_to_db()

#     # Assert
#     assert respx_mock.calls.call_count == 3, "Expected only two calls to check if artist and alias exist."

#     # verify if artist exists
#     assert respx_mock.calls[0].request.method == "GET", "Call to verify if an artist exist was not of type GET"
#     assert respx_mock.calls[0].request.url.params["name"] == artist.custom_name, "Call to verify if an artist exist used an unexpected parameter"
    
#     # verify if alias exists
#     assert respx_mock.calls[1].request.method == "GET", "Call to verify if an alias exist was not of type GET"
#     assert respx_mock.calls[1].request.url.params["name"] == artist.name, "Call to verify if an artist exist used an unexpected parameter"
#     assert respx_mock.calls[1].request.url.params["franchiseId"] == str(artist.product_id), "Call to verify if an artist exist used an unexpected parameter"

#     # post new alias
#     post_new_alias_call = respx_mock.calls[2].request 
#     assert post_new_alias_call.method == "POST", "Call to create new alias was not of type POST"
#     post_new_alias_call_content = json.loads(post_new_alias_call.content.decode())
#     assert post_new_alias_call_content == {'Name': artist.name, 'artistid': server_artist_id, 'franchiseid': '1'}, f"Post body to create new alias did not match expected object"


