# IOS Assignment 2 Backend

## Setup Instructions

### Configuration Setup

The `appsettings.Development.json` file contains sensitive configuration values (JWT keys, tokens, etc.). 

**For new team members:**
1. The file already exists in repo w/ placeholder values
2. Update the values in `appsettings.Development.json` with actual secrets:
   - Replace `YOUR_JWT_SECRET_KEY_HERE` with the JWT secret key 
   - Replace `YOUR_GITHUB_TOKEN_HERE` with GitHub personal access token

**Important**: 
- Git configured to ignore changes to `appsettings.Development.json` to prevent accidental commits of secrets
- Use the template file (`appsettings.Development.template.json`) as a reference for the expected structure
-> This is to make sure we never commit actual secrets to version control

## Running the Application

```bash
dotnet run
```

## API Endpoints

- Authentication: `/api/auth`
- AI Services: `/api/ai`