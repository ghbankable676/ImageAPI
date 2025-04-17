# Image API

This is an image API that allows users to upload, retrieve, and manage images, including generating resized image variations.

## Features

- **Upload Image**: Upload an image and store it on the server or in the cloud.
- **Retrieve Image Variations**: Generate and retrieve resized versions of images.
- **Metadata**: Each uploaded image has associated metadata.

## Installation

Follow these steps to run the API locally:

1. **Clone the repository**:
    ```bash
    git clone https://github.com/ghbankable676/ImageAPI
    cd ImageAPI
    ```

2. **Install dependencies**:
    - Visual Studio/VS Code should restore dependencies automatically.
    - Or run:
    ```bash
    dotnet restore
    ```

3. **Configure the app settings**:
    Ensure that `appsettings.json` - available at ImageAPI\ImageAPI - is correctly set up with your desired configuration
        ImageBasePath is the only required configuration to change - it sets the path to store images in the filesystem
        While there is an implementation for MongoDB, it was not tested and UseMongo should be set to false.

4. **Run the application**:
    ```bash
    cd ImageAPI
    dotnet run
    ```
    Ensure it's being run at the project level. The API will be available at `http://localhost:5091`.
    A testing interface is available through Swagger at http://localhost:5091/swagger/index.html

## API Endpoints

### Upload Image
**POST** `/api/images/upload`

- **Description**: Uploads an image.
- **Request**: `multipart/form-data` with the file as the body.
- **Response**: `200 OK` on success.

### Get Image
**GET** `/api/images/{imageId}`

- **Description**: Retrieves the original image.
- **Parameters**: `imageId` (Guid)
- **Response**: Path of the image file.

### Get Image Variation
**GET** `/api/images/{imageId}/variation/{height}`

- **Description**: Retrieves a resized version of the image.
- **Parameters**: `imageId` (Guid), `height` (int)
- **Response**: Path of the resized image.

### Delete Image
**DELETE** `/api/images/{imageId}`

- **Description**: Deletes an image and its variations.
- **Parameters**: `imageId` (Guid)
- **Response**: `200 OK` on success.

## Error Handling

The API uses standard HTTP status codes for errors - they are simplified but should be intuitive enough:

- `400 Bad Request`: Invalid request parameters.
- `404 Not Found`: Resource not found.
- `500 Internal Server Error`: Unexpected server error.

## Logging

Logs are saved in the `Logs` folder, with daily rolling filenames (e.g., `log-20250410.log`).

## Architecture overview

Components

    ImageAPI:
        Controllers (API Layer)
        Exposes HTTP endpoints for image upload, retrieval, and variation access using RESTful principles.

        Services (Business Logic)
        The ImageService handles the core logic for storing images, generating resized variations, validating dimensions, and interacting with the repository.
        The AspectRatioResolutionService is responsible for calculating the aspect ratio and determining the appropriate resolution for image variations.

        Repositories (Persistence Abstraction)
        Supports two interchangeable implementations for image metadata storage:

            In-Memory: ImageRepository.cs - A simple implementation using a dictionary and stored in the filesystem in a json file.

            MongoDB: ImageRepositoryMongo.cs For persistent metadata storage using Mongo (configurable via AppSettings, although not tested).

        Models
        Defines the data structures for images and their metadata.

        Storage Layer

            Image files are stored on disk using a configurable ImageBasePath.

            Local storage paths are dynamically created per image using GUIDs

    ImageAPITests
        Unit tests for the API, covering various scenarios for image upload, retrieval, and deletion.