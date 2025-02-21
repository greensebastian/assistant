# Itinerary Manager

This project is intended to make trip planning easier, by allowing users to combine AI and google maps capabilities to iteratively arrive at a good itinerary. It allows you to manage multiple itineraries, and plan trips by prompting OpenAi for suggestions and populating the itinerary with Google Maps data.

Example scenario:
1. Create new itinerary with name "My trip to italy".
1. Request suggestions with the prompt "I want to visit the best pizza places in Rome and Naples during easter 2025".
1. Apply or regenerate suggestions made by OpenAi.
1. Use generated itinerary to plan trip.
1. Change itinerary as you go by prompting for more suggestions.

## Roadmap

* [x] Basic querying
* [x] OpenAi integration
* [x] Maps integration (links and placeId)
* [x] Iterative changes to activities (add/remove/reschedule/reorder)
* [x] Non-json UI
* [x] Maps view and route
* [ ] Online access and collaboration
...

## Usage

### Setup

Api keys for **OpenAi gpt-4o-mini** and **Google Places (New)** are required to run the project.

They should be accessible to dotnet configuration at `OpenApi:ApiKey` and `GoogleMaps:ApiKey` respectively. This can be done through environment variables or user secrets.

#### User secrets
1. `cd backend/ItineraryManager.WebApi`
1. `dotnet user-secrets set "OpenApi:ApiKey" "<your-openai-key>"`
1. `dotnet user-secrets set "GoogleMaps:ApiKey" "<your-google-maps-key>"`

#### Environment variables
1. `export OpenApi__ApiKey=<your-openai-key>`
1. `export GoogleMaps__ApiKey=<your-google-maps-key>`

### Running

1. Ensure api keys are in place.
1. `cd backend`
1. `docker compose up -d`
1. `dotnet run --project ./ItineraryManager.WebApp/`
1. Visit app on http://localhost:5008
1. visit mongo-express on http://localhost:8081