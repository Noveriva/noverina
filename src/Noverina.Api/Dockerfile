FROM mcr.microsoft.com/dotnet/sdk:10.0.400 AS build
WORKDIR /src

COPY ["NuGet.Config", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/Noverina.Api/Noverina.Api.csproj", "src/Noverina.Api/"]
RUN dotnet restore "src/Noverina.Api/Noverina.Api.csproj"

COPY . .
WORKDIR "/src/src/Noverina.Api"
RUN dotnet publish "Noverina.Api.csproj" -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "Noverina.Api.dll"]
