FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

#copy all layers
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Presentation/Presentation.csproj", "src/Presentation/"]

#add packages
RUN dotnet restore "src/Presentation/Presentation.csproj"

COPY . .

#build project
WORKDIR "/src/src/Presentation"
RUN dotnet build "Presentation.csproj" -c Release -o /app/build

#publish
FROM build AS publish
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

EXPOSE 8080
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

USER $APP_UID

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Presentation.dll"]