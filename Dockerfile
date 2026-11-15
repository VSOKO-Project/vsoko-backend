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

#publish (собирает проект сам, отдельный dotnet build не нужен)
WORKDIR "/src/src/Presentation"
RUN dotnet publish "Presentation.csproj" -c Release -o /app/publish /p:UseAppHost=false --no-restore

RUN dotnet tool install --tool-path /tools dotnet-ef --version 10.0.15
RUN /tools/dotnet-ef migrations bundle \
    --project /src/src/Infrastructure/Infrastructure.csproj \
    --startup-project /src/src/Infrastructure/Infrastructure.csproj \
    --configuration Release --target-runtime linux-x64 --output /app/efbundle

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

EXPOSE 8080
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

USER $APP_UID

COPY --from=build /app/publish .
COPY --from=build --chmod=755 /app/efbundle ./efbundle

ENTRYPOINT ["dotnet", "Presentation.dll"]