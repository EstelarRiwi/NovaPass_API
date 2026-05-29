# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS build
WORKDIR /app

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /out

# Stage 2: Runtime only
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble
WORKDIR /app

COPY --from=build /out .

EXPOSE 8080

ENTRYPOINT ["dotnet", "NovaPass_API.dll"]