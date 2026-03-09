# 🌱 AgroSense Sensor Ingestion API

Microsserviço responsável pela **ingestão de dados de sensores** da plataforma **AgroSense** — um sistema de monitoramento agrícola inteligente baseado em sensores de campo.

---

## 📋 Sobre o Projeto

O **AgroSense Sensor Ingestion API** é um serviço da arquitetura de microsserviços do AgroSense, responsável por:

- Receber leituras brutas dos sensores agrícolas em campo
- Processar e validar os dados de ingestão (temperatura, umidade, solo, etc.)
- Publicar eventos de leitura via mensageria (RabbitMQ) para outros serviços consumirem
- Persistir o histórico de leituras no banco de dados

---

## 🏗️ Arquitetura

O serviço faz parte de um ecossistema de microsserviços implantado em **Kubernetes**, composto por:

| Serviço | Responsabilidade |
|---|---|
| `agrosense-api-gateway` | Roteamento e entrada de requisições externas |
| `agrosense-api-identity` | Autenticação e autorização |
| `agrosense-api-alert` | Gestão de alertas baseados em leituras |
| `agrosense-api-property` | Gestão de propriedades rurais |
| `agrosense-api-sensor` | **Este serviço** — ingestão de dados dos sensores |

---

## 🛠️ Tecnologias

- **.NET / C#** — Framework principal
- **PostgreSQL** — Banco de dados relacional (`postgres-sensor-ingestion`)
- **RabbitMQ** — Publicação de eventos de leitura para outros microsserviços
- **Docker** — Containerização
- **Kubernetes** — Orquestração de containers
- **Prometheus** — Coleta de métricas
- **Grafana + Loki** — Observabilidade e centralização de logs

---

## 🚀 Como Executar

### Pré-requisitos

- [.NET SDK](https://dotnet.microsoft.com/download) 10+
- [Docker](https://www.docker.com/)
- [kubectl](https://kubernetes.io/docs/tasks/tools/) (para deploy no cluster)

### Localmente

```bash
# Clone o repositório
git clone https://github.com/PeterHSS/agrosense-sensor-ingestion.git
cd agrosense-sensor-ingestion

# Restaure as dependências
dotnet restore

# Execute
dotnet run --project Api/
```

### Com Docker

```bash
# Build da imagem
docker build -t agrosense-sensor-ingestion .

# Execute o container
docker run -p 8080:80 agrosense-sensor-ingestion
```

---

## ☸️ Deploy no Kubernetes

O serviço é implantado no namespace `agrosense` como um `ClusterIP`:

```bash
# Verifique o serviço no cluster
kubectl get services -n agrosense

# Verifique os pods em execução
kubectl get pods -n agrosense -l app=agrosense-api-sensor

# Logs do serviço
kubectl logs -n agrosense -l app=agrosense-api-sensor --follow
```

O serviço está acessível internamente no cluster via:
```
http://agrosense-api-sensor.agrosense.svc.cluster.local:80
```

---

## 📊 Observabilidade

### Prometheus

Métricas expostas no endpoint `/metrics`, coletadas pelo Prometheus em:
```
http://prometheus:9090
```

### Grafana + Loki

Dashboards e logs centralizados acessíveis via Grafana. Datasources configurados:

| Fonte | URL interna |
|---|---|
| Prometheus | `http://prometheus:9090` |
| Loki | `http://loki:3100` |

---

## 🔁 CI/CD

O repositório utiliza **GitHub Actions** (`.github/workflows/`) para:

- Build e testes automáticos a cada push
- Build e push da imagem Docker para o registry
- Deploy automatizado no cluster Kubernetes

---

## 📁 Estrutura do Projeto

```
agrosense-sensor-ingestion/
├── .github/
│   └── workflows/                  # Pipelines de CI/CD
├── Api/
│   ├── Common/
│   │   └── Middlewares/            # Middlewares globais da aplicação
│   ├── Domain/
│   │   ├── Abstractions/
│   │   │   ├── Infrastructure/
│   │   │   │   └── Messaging/      # Interfaces de mensageria
│   │   │   └── UseCases/           # Interfaces dos casos de uso
│   │   ├── Entities/
│   │   │   └── Enums/              # Enumerações do domínio
│   │   └── Events/                 # Eventos de domínio
│   ├── Features/
│   │   └── Ingestion/              # Caso de uso: ingestão de leitura de sensor
│   ├── Infrastructure/
│   │   ├── Messaging/              # Integração com RabbitMQ
│   │   ├── Persistence/
│   │   │   ├── Configurations/     # Configurações do EF Core
│   │   │   ├── Contexts/           # DbContext
│   │   │   └── Migrations/         # Migrações do banco de dados
│   │   └── Settings/               # Configurações de infraestrutura
│   └── Properties/                 # Configurações do projeto .NET
├── .dockerignore
├── .gitignore
├── Agrosense.Sensor.Ingestion.slnx # Solution file
└── README.md
```

---

## 📄 Licença

Este projeto está licenciado sob a licença **MIT**. Consulte o arquivo [LICENSE](./LICENSE) para mais detalhes.
