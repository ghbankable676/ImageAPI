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
    git clone https://github.com/yourusername/image-api.git
    cd image-api
    ```

2. **Install dependencies**:
    - Visual Studio/VS Code should restore dependencies automatically.
    - Or run:
    ```bash
    dotnet restore
    ```

3. **Configure the app settings**:
    Ensure that `appsettings.json` is correctly set up with your desired configuration
        ImageBasePath is the only required configuration to change - it sets the path to store images in the filesystem
        While there is an implementation for MongoDB, it was not tested and UseMongo should be set to false.

4. **Run the application**:
    ```bash
    dotnet run
    ```
    The API will be available at `https://localhost:7039`.

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
- **Response**: The image file.

### Get Image Variation
**GET** `/api/images/{imageId}/variation/{height}`

- **Description**: Retrieves a resized version of the image.
- **Parameters**: `imageId` (Guid), `height` (int)
- **Response**: The resized image.

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