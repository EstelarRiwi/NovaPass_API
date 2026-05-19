# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Restore dependencies first — improves layer caching on subsequent builds
COPY *.csproj ./
RUN dotnet restore

# Copy source and publish
COPY . ./
RUN dotnet publish -c Release -o /out

# Stage 2: Runtime only
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

COPY --from=build /out .

EXPOSE 8080

# Replace "cicd" with your actual project name
ENTRYPOINT ["dotnet", "NovaPass_API.dll"]