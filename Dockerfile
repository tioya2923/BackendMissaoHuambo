# Dockerfile para aplicação ASP.NET Core no Render
# Etapa de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copia arquivos de projeto e restaura dependências
COPY *.csproj ./
RUN dotnet restore

# Copia o restante do código e publica
COPY . ./
RUN dotnet publish -c Release -o out

# Etapa de runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out ./

# Porta padrão do ASP.NET Core
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

# Comando de inicialização
ENTRYPOINT ["dotnet", "MissaoBackend.dll"]
