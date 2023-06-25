FROM mcr.microsoft.com/dotnet/runtime:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["ForwardRss/ForwardRss.csproj", "ForwardRss/"]
RUN dotnet restore "ForwardRss/ForwardRss.csproj"
COPY . .
WORKDIR "/src/ForwardRss"
RUN dotnet build "ForwardRss.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ForwardRss.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ForwardRss.dll"]