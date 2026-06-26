# TipMolde API

API REST em ASP.NET Core para gestao do ciclo de vida de moldes, cobrindo as areas comercial, desenho e producao. O projeto inclui autenticacao JWT, controlo de acesso por perfis, geracao de fichas/relatorios e uma bateria de testes unitarios e de integracao.

## O que a API cobre

- Autenticacao e logout com JWT
- Gestao de utilizadores e perfis (`ADMIN`, `GESTOR_COMERCIAL`, `GESTOR_DESENHO`, `GESTOR_PRODUCAO`)
- Clientes, encomendas, relacao encomenda-molde e fornecedores
- Moldes, pecas, maquinas, fases e registos de producao
- Projetos, revisoes e registos de tempo de projeto
- Fichas de producao, upload/download de documentos e exportacao de relatorios
- Endpoint de health check em `GET /api/health`

## Arquitetura da solucao

A solucao esta organizada em camadas:

- `TipMolde`: API ASP.NET Core, controllers, middleware e configuracao
- `TipMolde.Application`: casos de uso, DTOs, servicos e mapeamentos
- `TipMolde.Domain`: entidades e enums do dominio
- `TipMolde.Infrastructure`: EF Core, repositorios, servicos de ficheiros, JWT e relatorios
- `TipMolde.Tests`: testes unitarios e de integracao

## Stack tecnica

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core 8
- MySQL com `Pomelo.EntityFrameworkCore.MySql`
- JWT Bearer Authentication
- Swagger/OpenAPI
- ClosedXML para exportacao Excel
- QuestPDF para documentos PDF
- NUnit, Moq e FluentAssertions para testes

## Principais areas funcionais

- Comercial: `clientes`, `encomendas`, `encomenda-moldes`, `fornecedores`, `pedidos-material`
- Desenho: `projetos`, `revisoes`, `registos-tempo-projeto`
- Producao: `moldes`, `pecas`, `maquinas`, `fases-producao`, `registos-producao`
- Fichas e relatorios: `fichas-producao`, documentos anexos e exportacoes FLT/FRE/FRM/FRA/FOP

## Requisitos

Para correr o projeto localmente vais precisar de:

- .NET 8 SDK
- MySQL acessivel pela API
- Uma base de dados preparada para a aplicacao
- Pastas locais para `Storage` e `Templates`

## Configuracao

O projeto le configuracao de `appsettings.json`, variaveis de ambiente e `user-secrets`.

### 1. Configurar a connection string

O nome esperado e `ConnectionStrings:DefaultConnection`.

Exemplo com `user-secrets`:

```powershell
dotnet user-secrets --project TipMolde/TipMolde.API.csproj set "ConnectionStrings:DefaultConnection" "Server=localhost;Port=3306;Database=tipmolde;Uid=root;Pwd=your_password;"
```

Tambem podes usar variavel de ambiente:

```powershell
$env:ConnectionStrings__DefaultConnection="Server=localhost;Port=3306;Database=tipmolde;Uid=root;Pwd=your_password;"
```

### 2. Configurar JWT

A API valida a secao `Jwt` no arranque. A chave `Jwt:SecretKey` tem de ter pelo menos 32 caracteres.

Exemplo com `user-secrets`:

```powershell
dotnet user-secrets --project TipMolde/TipMolde.API.csproj set "Jwt:SecretKey" "uma_chave_forte_com_mais_de_32_caracteres"
dotnet user-secrets --project TipMolde/TipMolde.API.csproj set "Jwt:Issuer" "TipMolde.Api"
dotnet user-secrets --project TipMolde/TipMolde.API.csproj set "Jwt:Audience" "TipMolde.Client"
```

### 3. Configurar CORS

Em desenvolvimento, se `Cors:AllowedOrigins` estiver vazio, a API aceita qualquer origem. Em ambientes nao-development convem definir explicitamente as origens permitidas.

Exemplo por variavel de ambiente:

```powershell
$env:Cors__AllowedOrigins__0="http://localhost:3000"
```

### 4. Criar as pastas esperadas

As paths por defeito sao:

- `Storage/Fichas`
- `Storage/Uploads`
- `Templates`

No estado atual do repositorio estas pastas nao estao versionadas, por isso devem ser criadas localmente.

### 5. Disponibilizar os templates

Por defeito a API espera encontrar estes ficheiros dentro de `Templates`:

- `FLT.xlsx`
- `FRE.xlsx`
- `FRM.xlsx`
- `FRA.xlsx`
- `FOP.xlsx`

Os nomes podem ser ajustados em [TipMolde/appsettings.json](/C:/Users/HP/Documents/TipMolde/Aplicacao/TipMolde/TipMolde/appsettings.json:1).

### 6. Nota importante sobre a base de dados

Neste repositorio nao existem migrations EF Core nem scripts SQL versionados. A API tambem nao executa `Database.Migrate()` no arranque. Isso significa que a base de dados e o schema precisam de estar preparados antes de iniciar a aplicacao.

## Como executar

### Executar localmente

```powershell
dotnet restore
dotnet build TipMolde.sln
dotnet run --project TipMolde/TipMolde.API.csproj
```

Em `Development`, o `launchSettings.json` define:

- Swagger: `https://localhost:57663/swagger`
- Alternativa HTTP: `http://localhost:57664/swagger`

Health check:

- `GET http://localhost:57664/api/health`

## Testes

Para correr os testes:

```powershell
dotnet test TipMolde.Tests/TipMolde.Tests.csproj
```

Notas:

- Existem testes unitarios e testes de integracao
- Alguns testes de integracao com MySQL usam a variavel `TIPMOLDE_TEST_DB_CONNECTION`
- Em alternativa, esses testes leem `ConnectionStrings:DefaultConnection` dos `user-secrets` do projeto `TipMolde.API`

Exemplo:

```powershell
$env:TIPMOLDE_TEST_DB_CONNECTION="Server=localhost;Port=3306;Database=tipmolde_tests;Uid=root;Pwd=your_password;"
dotnet test TipMolde.Tests/TipMolde.Tests.csproj
```

## Docker

Existe um `Dockerfile` para publicar a API em .NET 8.

Se quiseres arrancar tudo com `compose.yaml`, o fluxo recomendado e:

1. Garantir que existe um `.env` na mesma pasta do `compose.yaml`.
2. Preencher os valores reais no `.env`.
3. Garantir que `tipmolde-api:latest` foi carregada a partir do `tipmolde-api.tar`.
4. Executar `docker compose up -d` a partir da pasta `TipMolde`.

Build:

```powershell
docker build -t tipmolde-api .
```

Run:

```powershell
docker run -p 8080:8080 `
  -e ASPNETCORE_ENVIRONMENT=Production `
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Port=3306;Database=tipmolde;Uid=root;Pwd=your_password;" `
  -e Jwt__SecretKey="uma_chave_forte_com_mais_de_32_caracteres" `
  -e Jwt__Issuer="TipMolde.Api" `
  -e Jwt__Audience="TipMolde.Client" `
  -v "${PWD}\\Storage:/app/Storage" `
  -v "${PWD}\\Templates:/app/Templates" `
  tipmolde-api
```

Como os templates e ficheiros de storage nao fazem parte da imagem publicada por defeito, o mais seguro e monta-los como volumes.

## Estrutura do repositorio

```text
.
|-- TipMolde.sln
|-- TipMolde/
|-- TipMolde.Application/
|-- TipMolde.Domain/
|-- TipMolde.Infrastructure/
`-- TipMolde.Tests/
```

## Observacoes uteis

- O Swagger so e ativado automaticamente em ambiente `Development`
- Em producao a API faz `UseHttpsRedirection()`
- A autenticacao e baseada em JWT com revogacao de token no logout
- O endpoint `POST /api/auth/login` devolve token e data de expiracao
