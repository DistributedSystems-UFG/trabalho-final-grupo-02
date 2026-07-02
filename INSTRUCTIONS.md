**Disciplina: Software Concorrente e Distribuído**  

**Trabalho Final da Disciplina**  
**– Projeto e Implementação de um Sistema de Software Concorrente e Distribuído –**

## Objetivo

O objetivo do trabalho é exercitar, de forma integrada, os conceitos de sistemas distribuídos e programação concorrente na construção de um sistema de software. O trabalho deve explorar métodos e padrões para solução dos principais problemas de concorrência e distribuição, fazendo uso de tecnologias e ferramentas de relevância atual.

## Visão geral

O sistema a ser desenvolvido deve conter elementos de sistemas distribuídos e de programação concorrente, envolvendo diferentes modelos de programação (com o emprego de mais de uma linguagem de programação) e paradigmas de interação (cliente servidor, publish-subscribe, messaging).

O sistema deve conter as seguintes características, independentemente do cenário de aplicação:

- Serviço acessível a múltiplos clientes na Internet;  
- Serviço constituído por meio da integração e coordenação de vários componentes distribuídos, os quais devem ser implementados como parte do trabalho;  
- Acessos concorrentes a recursos/dados compartilhados;  
- Processamento dos dados no lado servidor, concorrentemente com os acessos dos clientes;  
- Uso de mecanismos de interação remota síncrona (bloqueante) e assíncrona;  
- Replicação e particionamento de dados e funcionalidades; e  
- Tratamentos para garantir consistência de dados e disponibilidade das funcionalidades.

A elaboração do cenário de aplicação específico, incluindo requisitos e arquitetura, é parte integrante do trabalho.

## Exemplos de cenários de aplicação

1. Base de dados compartilhada, que pode ser acessada simultaneamente por diferentes clientes remotos, com operações para acesso aos dados (minimamente, CRUD), notificação de eventos (p. ex., relativos a parâmetros de desempenho) para um dashboard de administração e operações automáticas de manutenção (ex.: ordenação, sanitização dos dados, consistência etc.).  
2. Editor de documentos compartilhados, com funcionalidades para visualização e edição remota por vários clientes simultâneos, notificação de eventos de edição para os demais clientes e operações de processamento em background (ex.: corretor ortográfico, formatador de texto).  
3. Jogo online multijogador, no qual múltiplos jogadores podem, simultaneamente, visualizar o estado compartilhado do jogo, executar ações que modificam esse estado e receber notificações de mudanças no estado feitas por outros jogadores ou por operações internas de manutenção das regras do jogo.  
4. Sistema de controle de inventário, no qual múltiplos vendedores e compradores podem, simultaneamente, realizar operações de saída (venda) ou entrada (compra) de produtos, com suporte para alertas (ex.: produtos com baixa quantidade) e para operações internas de manutenção do inventário (ex.: para reconciliação dos quantitativos após perdas).

## Formato

* Trabalho em grupo de 3-4 alunos.  
* Implementação dos serviços do sistema com as características acima descritas, juntamente com clientes simulados para demonstrar seu uso.  
* Criar um cenário de demonstração representativo, que permita exercitar, sistematicamente, as características do sistema.  
* Executar a demonstração utilizando a nuvem do AWS (EC2).   
* Artefatos a serem entregues: código-fonte (e executáveis); documentação (da arquitetura e implementação); instruções de uso (readme); dados de teste; e vídeo de demonstração (com a participação efetiva de todos os integrantes do grupo).

## Entrega

Data: 28/06/2026  
Código, dados de teste e instruções: via GitHub Classroom  
Documentação e vídeo: via Plataforma Turing