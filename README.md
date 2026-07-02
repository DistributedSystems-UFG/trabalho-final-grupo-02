# Sistema de Controle de Inventário Distribuído

Trabalho Final da disciplina de Software Concorrente e Distribuído (SCD) — UFG 2026.1.  
Professor: Fábio Moreira Costa
Grupo 2: LÍBNA RAFFAELY DE JESUS COSTA, LUCAS MOREIRA IGLESIAS e VICTOR GABRIEL PACHECO GONTIJO

## Visão Geral

Este projeto implementa um Sistema de Controle de Inventário fortemente consistente, projetado para demonstrar conceitos avançados de concorrência e sistemas distribuídos. O objetivo principal é demonstrar o tratamento correto de acessos concorrentes em dados compartilhados e integrações assíncronas/síncronas.

A arquitetura engloba:
- **API Gateway** (.NET 8): Ponto de entrada REST voltado para os clientes, converte para gRPC.
- **Inventory Service** (.NET 8): Serviço core (gRPC server) e acesso ao banco principal (PostgreSQL) usando concorrência otimista (optimistic concurrency via PostgreSQL `xmin`). Emite os eventos transacionais no RabbitMQ.
- **Dashboard Admin** (Python FastAPI): Interface Web Real-time que consome a fila do RabbitMQ e lê estatísticas de uma réplica do banco de dados (Eventual Consistency).
- **Maintenance Worker** (Python): Processamento background assíncrono para reconciliação automática de estoque baseada nos eventos `stock.low`.
- **PostgreSQL**: Streaming replication local (Primary para leitura/escrita garantidas; Replica para relatórios da Web).
- **RabbitMQ**: Message broker assíncrono com padrão Publish/Subscribe (`topic`).
- **Client Simulator** (.NET 8 Console): Ferramenta interativa e simulador massivo de carga.

## Pré-requisitos

- **Docker** e **Docker Compose**
- **.NET 8 SDK** (para executar a simulação CLI fora dos contêineres Docker)

## Como Executar

A infraestrutura inteira e todos os microserviços estão definidos nos arquivos `docker-compose.yml`.

1. Clone o repositório e vá para a raiz do projeto.
2. Inicie a infraestrutura:
   ```bash
   cd infra
   docker-compose up --build -d
   ```
3. Os seguintes serviços estarão expostos localmente:
   - **API Gateway (REST)**: `http://localhost:8081` (Swagger em `/swagger`)
   - **Admin Dashboard**: `http://localhost:8000`
   - **RabbitMQ Management**: `http://localhost:15672` (user: admin / pass: admin)
   - **PostgreSQL Primary**: `localhost:5432` (user: postgres / pass: postgres)
   - **PostgreSQL Replica**: `localhost:5433` (user: postgres / pass: postgres)

## Como Executar na Nuvem (AWS EC2)

Para a entrega e demonstração oficial, o sistema deve ser hospedado no AWS EC2.

1. Provisione uma instância EC2 (ex: Ubuntu 24.04 LTS).
2. Libere as portas `80` (API Gateway), `8000` (Dashboard) e `15672` (RabbitMQ UI) no Security Group da AWS.
3. Instale o Docker e o Docker Compose na instância.
4. Clone o repositório na instância EC2.
5. Inicie a infraestrutura combinando o arquivo base com o de produção:
   ```bash
   cd infra
   docker-compose -f docker-compose.yml -f docker-compose.prod.yml up --build -d
   ```
6. O **API Gateway** estará disponível no IP público da instância na porta `80` (ex: `http://<IP-EC2>/swagger`).
7. O **Admin Dashboard** estará disponível em `http://<IP-EC2>:8000`.

*(Nota: Quando for usar o Client CLI localmente apontando para a nuvem, altere a `BaseAddress` no `Program.cs` para o IP público do EC2).*

## Roteiro de Avaliação (Para o Professor)

Professor Fábio, preparamos este roteiro de demonstração end-to-end para facilitar a avaliação técnica dos requisitos da disciplina (SCD 2026.1). Executando os passos a seguir, é possível validar claramente o comportamento do sistema sob alta concorrência e a comunicação entre os componentes distribuídos.

**1. Preparação da Infraestrutura**
- Execute `docker-compose up` no diretório `/infra`.
- **Validação:** Todos os serviços (API Gateway, Inventory Service, RabbitMQ, PostgreSQL Primary/Replica, Dashboard e Maintenance Worker) devem inicializar e constar como saudáveis.

**2. Consulta Inicial e Verificação da Base**
- Em um terminal, inicie o **Client CLI**:
  ```bash
  cd src/Client
  dotnet run
  ```
- No menu do CLI, escolha a opção para listar produtos.
- **Validação:** A listagem deve retornar 5 produtos com as quantidades iniciais exatas fornecidas no script de seed, comprovando a comunicação (Client REST → API Gateway → gRPC → Inventory Service → PostgreSQL).

**3. Simulação de Carga e Concorrência Otimista (Core Concept)**
- No Client CLI, acesse o modo de simulação (**Simulate Load**).
- Dispare 20 vendas concorrentes simultâneas do Produto #1 (que possui exatas 10 unidades no estoque inicial), solicitando 1 unidade por requisição.
- **Validação:** 
  - Aproximadamente 10 requisições retornarão Sucesso (HTTP 200) e o restante falhará apropriadamente por conflito (HTTP 409) ou sem estoque (HTTP 400).
  - O estoque final não poderá ser menor que zero.
  - Isso demonstra o mecanismo de **Optimistic Locking (`RowVersion`)** do EF Core e Postgres (`xmin`) barrando atualizações de estado inconsistentes ("lost updates").
  - Verifique o **Admin Dashboard** (`http://localhost:8000`): alertas de eventos `product.sold` e `stock.low` aparecerão em tempo real graças aos WebSockets alimentados pelo **RabbitMQ** (integração remota assíncrona).

**4. Reposição de Estoque (Restock)**
- Usando o CLI, compre (Buy) 20 unidades do Produto #1.
- **Validação:** O estoque do banco de dados será devidamente restaurado. No Dashboard, será perceptível o evento `product.bought` publicado assim que a transação for efetivada (*publish-after-commit*).

**5. Processamento em Background e Filas Duráveis**
- Interrompa o contêiner do `maintenance-worker` via Docker.
- No CLI, force vendas adicionais até engatilhar mais 5 alertas do tipo `stock.low`.
- Acesse a interface do RabbitMQ Management (`http://localhost:15672`).
- **Validação:** Na fila durável `maintenance.tasks`, devem existir exatamente 5 mensagens represadas, ilustrando a tolerância a falhas na mensageria.

**6. Retomada do Worker Assíncrono**
- Reinicie o contêiner do `maintenance-worker`.
- **Validação:** Imediatamente, o worker processará as mensagens represadas de forma concorrente às ações do Gateway. Você poderá verificar a gravação de 5 novas linhas de reconciliação no banco de dados.

**7. Eventual Consistency e Particionamento de Dados**
- Conecte-se e faça uma consulta SQL diretamente na **réplica do PostgreSQL** (`localhost:5433`).
- **Validação:** A réplica refletirá os mesmos dados da primária, com um possível retardo aceitável (lag), justificando a leitura separada pelo Dashboard para aliviar o servidor de escrita (Padrão de Replicação).

## Estrutura do Repositório

- `/infra`: Manifestos Docker e configuração nativa do PostgreSQL.
- `/src/ApiGateway`: Microsserviço de exposição Web API C#.
- `/src/InventoryService`: Backend de processamento e gRPC server.
- `/src/dashboard`: Serviço Python do administrador de sistemas.
- `/src/maintenance_worker`: Job Python assíncrono.
- `/src/Client`: Cliente interativo.

## Autores
*Trabalho desenvolvido como requisito da disciplina de SCD (2026.1)*
