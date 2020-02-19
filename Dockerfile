FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY ./src/Losol.Identity/*.csproj  ./src/Losol.Identity/
COPY ./src/Losol.Identity.Model/*.csproj ./src/Losol.Identity.Model/
COPY ./src/Losol.Identity.Services/*.csproj ./src/Losol.Identity.Services/

RUN cd /app/src \
  && dotnet restore Losol.Identity/Losol.Identity.csproj \
  && dotnet restore Losol.Identity.Model/Losol.Identity.Model.csproj \
  && dotnet restore Losol.Identity.Services/Losol.Identity.Services.csproj

# Copy everything else and build
COPY . ./
RUN dotnet publish /app/src/Losol.Identity/Losol.Identity.csproj -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build /app/out .
ENTRYPOINT ["dotnet", "Losol.Identity.dll"]