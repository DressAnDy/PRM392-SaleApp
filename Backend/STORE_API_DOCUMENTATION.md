?# STORE LOCATION API DOCUMENTATION

## Overview

He thong API de lay thong tin cua hang va tim kiem cua hang gan dia diem hien tai.

### Base URL
```
http://localhost:5000/api/stores
```

---

## API ENDPOINTS

### 1. GET ALL STORES

**Purpose:** Lay danh sach tat ca cua hang

**Method:** GET

**URL:** 
```
http://localhost:5000/api/stores
```

**Headers:**
```
Content-Type: application/json
```

**Query Parameters:** (None)

**Request Body:** (Empty)

**Example cURL:**
```bash
curl -X GET "http://localhost:5000/api/stores" \
  -H "Content-Type: application/json"
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Get all store locations successfully",
  "data": [
    {
      "locationId": 1,
      "address": "123 Duong Le Loi, Quan Hoan Kiem, Ha Noi",
      "latitude": 21.0285,
      "longitude": 105.8542
    },
    {
      "locationId": 2,
      "address": "456 Duong Nguyen Hue, Quan 1, TP Ho Chi Minh",
      "latitude": 10.7769,
      "longitude": 106.7009
    },
    {
      "locationId": 3,
      "address": "789 Duong Tran Hung Dao, Quan Hai Chau, Da Nang",
      "latitude": 16.0678,
      "longitude": 108.2261
    }
  ]
}
```

**Error Response (500 Internal Server Error):**
```json
{
  "success": false,
  "message": "Error retrieving store locations"
}
```

**Status Codes:**
| Code | Description |
|------|-------------|
| 200 | OK - Lay du lieu thanh cong |
| 500 | Server Error - Loi server |

---

### 2. GET STORE BY ID

**Purpose:** Lay chi tiet thong tin mot cua hang

**Method:** GET

**URL:**
```
http://localhost:5000/api/stores/{id}
```

**Path Parameters:**
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| id | integer | Yes | ID cua hang (Vi du: 1) |

**Headers:**
```
Content-Type: application/json
```

**Query Parameters:** (None)

**Request Body:** (Empty)

**Example cURL:**
```bash
curl -X GET "http://localhost:5000/api/stores/1" \
  -H "Content-Type: application/json"
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Get store location details successfully",
  "data": {
    "locationId": 1,
    "address": "123 Duong Le Loi, Quan Hoan Kiem, Ha Noi",
    "latitude": 21.0285,
    "longitude": 105.8542
  }
}
```

**Error Response - Store Not Found (404):**
```json
{
  "success": false,
  "message": "Store location not found"
}
```

**Error Response - Server Error (500):**
```json
{
  "success": false,
  "message": "Error retrieving store location"
}
```

**Status Codes:**
| Code | Description |
|------|-------------|
| 200 | OK - Tim thay cua hang |
| 404 | Not Found - Cua hang khong ton tai |
| 500 | Server Error - Loi server |

---

### 3. SEARCH NEARBY STORES

**Purpose:** Tim kiem cua hang gan dia diem hien tai

**Method:** GET

**URL:**
```
http://localhost:5000/api/stores/search/nearby
```

**Query Parameters:**
| Parameter | Type | Required | Default | Range | Description |
|-----------|------|----------|---------|-------|-------------|
| latitude | decimal | Yes | - | -90 to 90 | Vi do cua diem can tim |
| longitude | decimal | Yes | - | -180 to 180 | Kinh do cua diem can tim |
| radius | integer | No | 5 | > 0 | Ban kinh tim kiem (km) |

**Headers:**
```
Content-Type: application/json
```

**Request Body:** (Empty)

**Example cURL - With Default Radius:**
```bash
curl -X GET "http://localhost:5000/api/stores/search/nearby?latitude=21.0285&longitude=105.8542" \
  -H "Content-Type: application/json"
```

**Example cURL - With Custom Radius:**
```bash
curl -X GET "http://localhost:5000/api/stores/search/nearby?latitude=21.0285&longitude=105.8542&radius=10" \
  -H "Content-Type: application/json"
```

**Success Response (200 OK):**
```json
{
  "success": true,
  "message": "Search nearby stores successfully",
  "data": [
    {
      "locationId": 1,
      "address": "123 Duong Le Loi, Quan Hoan Kiem, Ha Noi",
      "latitude": 21.0285,
      "longitude": 105.8542,
      "distance": 0.5
    },
    {
      "locationId": 2,
      "address": "456 Duong Nguyen Hue, Quan Hoan Kiem, Ha Noi",
      "latitude": 21.032,
      "longitude": 105.86,
      "distance": 1.2
    },
    {
      "locationId": 3,
      "address": "789 Pho Hue, Quan Hai Ba Trung, Ha Noi",
      "latitude": 21.01,
      "longitude": 105.845,
      "distance": 2.8
    }
  ]
}
```

**Note:** Danh sach da duoc sap xep tu gan den xa (sort by distance ascending)

**Error Response - Invalid Coordinates (400):**
```json
{
  "success": false,
  "message": "Invalid latitude or longitude values"
}
```

**Error Response - Invalid Radius (400):**
```json
{
  "success": false,
  "message": "Radius must be greater than 0"
}
```

**Error Response - Server Error (500):**
```json
{
  "success": false,
  "message": "Error searching nearby stores"
}
```

**Status Codes:**
| Code | Description |
|------|-------------|
| 200 | OK - Tim kiem thanh cong |
| 400 | Bad Request - Tham so khong hop le |
| 500 | Server Error - Loi server |

---

## DATA MODELS

### StoreLocationDto

**Description:** Doan thong tin cua hang co ban

**Fields:**
```json
{
  "locationId": 1,
  "address": "string",
  "latitude": 21.0285,
  "longitude": 105.8542
}
```

| Field | Type | Description |
|-------|------|-------------|
| locationId | integer | ID cua hang (duy nhat) |
| address | string | Dia chi cua hang |
| latitude | decimal | Vi do (chính xác 6 chu so thap phan) |
| longitude | decimal | Kinh do (chính xác 6 chu so thap phan) |

**Example:**
```json
{
  "locationId": 1,
  "address": "123 Duong Le Loi, Quan Hoan Kiem, Ha Noi",
  "latitude": 21.028500,
  "longitude": 105.854200
}
```

---

### StoreLocationWithDistanceDto

**Description:** Thong tin cua hang voi khoang cach (chi dung trong search API)

**Fields:**
```json
{
  "locationId": 1,
  "address": "string",
  "latitude": 21.0285,
  "longitude": 105.8542,
  "distance": 0.5
}
```

| Field | Type | Description |
|-------|------|-------------|
| locationId | integer | ID cua hang |
| address | string | Dia chi cua hang |
| latitude | decimal | Vi do |
| longitude | decimal | Kinh do |
| distance | double | Khoang cach tu vi tri tim kiem (km) |

**Example:**
```json
{
  "locationId": 1,
  "address": "123 Duong Le Loi, Ha Noi",
  "latitude": 21.028500,
  "longitude": 105.854200,
  "distance": 0.5
}
```

---

## RESPONSE FORMAT

### Success Response

```json
{
  "success": true,
  "message": "String message for user",
  "data": null or {} or []
}
```

| Field | Type | Description |
|-------|------|-------------|
| success | boolean | true neu thanh cong, false neu loi |
| message | string | Thong bao cho user |
| data | any | Du lieu tra ve (object, array, hoac null) |

---

### Error Response

```json
{
  "success": false,
  "message": "String error message"
}
```

| Field | Type | Description |
|-------|------|-------------|
| success | boolean | Luon la false |
| message | string | Mo ta loi |

---

## COORDINATES REFERENCE

### Latitude (Vi do)

- Range: -90 (Nam Cuc) den +90 (Bac Cuc)
- Positive = Bac
- Negative = Nam
- Vietnam: ~8 den 23

### Longitude (Kinh do)

- Range: -180 (Tay) den +180 (Dong)
- Positive = Dong
- Negative = Tay
- Vietnam: ~102 den 109

### Vietnam Cities Examples

| City | Latitude | Longitude |
|------|----------|-----------|
| Ha Noi | 21.0285 | 105.8542 |
| TP Ho Chi Minh | 10.7769 | 106.7009 |
| Da Nang | 16.0678 | 108.2261 |
| Can Tho | 10.0379 | 105.7869 |
| Hai Phong | 20.8449 | 106.6880 |

---

## DISTANCE CALCULATION

**Formula Used:** Haversine Formula

```
Distance (km) = 2 * R * arcsin(sqrt(sin²(?lat/2) + cos(lat1) * cos(lat2) * sin²(?lon/2)))

R = 6371 km (Earth's radius)
?lat = lat2 - lat1
?lon = lon2 - lon1
```

**Example:**
- User at: 21.0285, 105.8542
- Store at: 21.0350, 105.8600
- Distance: ~0.75 km

---

## HTTP STATUS CODES

| Code | Status | Meaning |
|------|--------|---------|
| 200 | OK | Request successful |
| 400 | Bad Request | Invalid parameters |
| 404 | Not Found | Resource not found |
| 500 | Internal Server Error | Server error |

---

## ERROR MESSAGES

| Scenario | Status | Message |
|----------|--------|---------|
| Get all stores success | 200 | "Get all store locations successfully" |
| Get store by id success | 200 | "Get store location details successfully" |
| Search nearby success | 200 | "Search nearby stores successfully" |
| Store not found | 404 | "Store location not found" |
| Invalid latitude/longitude | 400 | "Invalid latitude or longitude values" |
| Invalid radius | 400 | "Radius must be greater than 0" |
| Server error on get all | 500 | "Error retrieving store locations" |
| Server error on get by id | 500 | "Error retrieving store location" |
| Server error on search | 500 | "Error searching nearby stores" |

---

## VALIDATION RULES

### Latitude Validation
- Must be between -90 and 90
- Invalid: -91, 91, 100, -150
- Valid: -85.5, 0, 21.0285, 45.5

### Longitude Validation
- Must be between -180 and 180
- Invalid: -185, 181, 200, -250
- Valid: -170, 0, 105.8542, 120.5

### Radius Validation
- Must be greater than 0
- Invalid: 0, -5, -10
- Valid: 1, 5, 10, 50, 100

---

## JAVASCRIPT EXAMPLE

### Get All Stores
```javascript
async function getAllStores() {
  const response = await fetch('http://localhost:5000/api/stores');
  const json = await response.json();
  
  if (json.success) {
    console.log('Stores:', json.data);
    json.data.forEach(store => {
      console.log(store.address);
    });
  } else {
    console.error('Error:', json.message);
  }
}

getAllStores();
```

### Get Store By ID
```javascript
async function getStoreById(id) {
  const response = await fetch(`http://localhost:5000/api/stores/${id}`);
  const json = await response.json();
  
  if (json.success) {
    console.log('Store:', json.data);
  } else {
    console.error('Error:', json.message);
  }
}

getStoreById(1);
```

### Search Nearby Stores
```javascript
async function searchNearby(latitude, longitude, radius = 5) {
  const url = new URL('http://localhost:5000/api/stores/search/nearby');
  url.searchParams.append('latitude', latitude);
  url.searchParams.append('longitude', longitude);
  url.searchParams.append('radius', radius);
  
  const response = await fetch(url);
  const json = await response.json();
  
  if (json.success) {
    console.log('Nearby stores (sorted by distance):');
    json.data.forEach(store => {
      console.log(`${store.address} - ${store.distance} km`);
    });
  } else {
    console.error('Error:', json.message);
  }
}

searchNearby(21.0285, 105.8542, 5);
```

---

## KOTLIN ANDROID EXAMPLE

### Data Classes
```kotlin
data class ApiResponse<T>(
    val success: Boolean,
    val message: String,
    val data: T?
)

data class Store(
    val locationId: Int,
    val address: String,
    val latitude: Double,
    val longitude: Double,
    val distance: Double? = null
)
```

### Retrofit Interface
```kotlin
interface StoreApi {
    @GET("stores")
    suspend fun getAllStores(): ApiResponse<List<Store>>

    @GET("stores/{id}")
    suspend fun getStoreById(@Path("id") id: Int): ApiResponse<Store>

    @GET("stores/search/nearby")
    suspend fun searchNearby(
        @Query("latitude") lat: Double,
        @Query("longitude") lng: Double,
        @Query("radius") radius: Int = 5
    ): ApiResponse<List<Store>>
}
```

### Usage
```kotlin
val api = retrofit.create(StoreApi::class.java)

// Get all stores
viewModelScope.launch {
    try {
        val response = api.getAllStores()
        if (response.success) {
            val stores = response.data
            stores.forEach { store ->
                Log.d("Store", store.address)
            }
        }
    } catch (e: Exception) {
        Log.e("Error", e.message)
    }
}

// Search nearby
viewModelScope.launch {
    try {
        val response = api.searchNearby(21.0285, 105.8542, 5)
        if (response.success) {
            val nearbyStores = response.data
            nearbyStores?.forEach { store ->
                Log.d("Store", "${store.address} - ${store.distance} km")
            }
        }
    } catch (e: Exception) {
        Log.e("Error", e.message)
    }
}
```

---

## IMPLEMENTATION CHECKLIST

- [ ] Call GET /api/stores when app starts
- [ ] Display stores as markers on Google Maps
- [ ] Get user GPS location via geolocation
- [ ] Call GET /api/stores/search/nearby with user coordinates
- [ ] Display nearby stores sorted by distance
- [ ] Handle all error responses
- [ ] Add loading indicator
- [ ] Cache store data locally
- [ ] Request location permission (Android/iOS)
- [ ] Test with different radius values

---

## NOTES

- All responses follow: { success, message, data }
- Coordinates in decimal format (6 decimal places precision)
- Search results automatically sorted nearest to farthest
- Distance calculated using Haversine formula
- Authentication is optional (Bearer token)
- No pagination implemented (suitable for small store counts)
- Server calculates distance, no client-side calculation needed

---

## SUPPORT

For issues or questions:
1. Check coordinates range is valid
2. Verify radius > 0
3. Check network connection
4. Review server logs for details

---

Last Updated: 2024
Status: Ready for Production
Backend Version: .NET 8
