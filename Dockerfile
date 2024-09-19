# Use the official .NET 7 SDK image to build the project
FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build

# Set the working directory inside the container
WORKDIR /src

# Copy the .csproj files and restore dependencies
COPY ./Ai.Orchestrator/*.sln ./
COPY ./Ai.Orchestrator/Ai.Orchestrator.csproj ./Ai.Orchestrator/
COPY ./Ai.Orchestrator.Common/Ai.Orchestrator.Common.csproj ./Ai.Orchestrator.Common/
COPY ./Ai.Orchestrator.Models/Ai.Orchestrator.Models.csproj ./Ai.Orchestrator.Models/
COPY ./Ai.Orchestrator.Plugins.Email/Ai.Orchestrator.Plugins.Email.csproj ./Ai.Orchestrator.Plugins.Email/
COPY ./Ai.Orchestrator.Plugins.Webhook/Ai.Orchestrator.Plugins.Webhook.csproj ./Ai.Orchestrator.Plugins.Webhook/
# COPY Ai.Orchestrator.Services/Ai.Orchestrator.Services.csproj ./Ai.Orchestrator.Services/

# Restore the dependencies
RUN dotnet restore Ai.Orchestrator/Ai.Orchestrator.csproj

# Copy the entire solution into the container
COPY . .

# Build the Ai.Orchestrator project
RUN dotnet publish ./Ai.Orchestrator/Ai.Orchestrator.csproj -c Release -o /out

# Use the runtime-only image to keep the final image size smaller
FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS runtime

# Set the working directory inside the runtime container
WORKDIR /app

# Copy the compiled application from the build stage
COPY --from=build /out .

# Expose the port the app will run on (based on your app’s configuration)
EXPOSE 80

# Set the entrypoint to run the Web API
ENTRYPOINT ["dotnet", "Ai.Orchestrator.dll"]