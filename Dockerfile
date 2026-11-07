# Turnos de policlínica — Matías Nicolás Echeveste Cardozo
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /fuente
COPY nuget.config global.json ./
COPY src/Turnos.Api/Turnos.Api.csproj src/Turnos.Api/
RUN dotnet restore src/Turnos.Api/Turnos.Api.csproj
COPY src src
RUN dotnet publish src/Turnos.Api/Turnos.Api.csproj -c Release -o /publicado --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=compilacion /publicado .
# La imagen oficial ya define el usuario sin privilegios "app".
USER app
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ConnectionStrings__Turnos="Data Source=/datos/turnos.db"
VOLUME /datos
EXPOSE 8080
ENTRYPOINT ["dotnet", "Turnos.Api.dll"]
