# Arquitetura

Esta versão é uma aplicação desktop WPF para Windows 10/11, x64, em C#/.NET 10. O desenvolvimento e os testes portáveis ocorreram em Linux; compilar para Windows não comprova que uma integração Win32 funciona em hardware.

## Componentes e dependências

- `UniversalMouse.Core`: motor de movimento, quantização, perfis JSON, capacidades e diagnósticos. Sem dependência de WPF ou Win32; testes executáveis no Linux e Windows.
- `UniversalMouse.Windows`: Raw Input, leitura de identidade, preferências globais do Windows, hook de mouse e reinjeção. Referencia Core.
- `UniversalMouse.App`: apresentação WPF em português brasileiro, estado de interface e comandos; referencia Core e Windows.
- `tests/UniversalMouse.Tests`: testes determinísticos do movimento e benchmark reproduzível.
- `tests/UniversalMouse.ProfileTests`: persistência, validação, capacidades e diagnósticos.

A estrutura foi reduzida a três assemblies para evitar projetos vazios. As responsabilidades permanecem separadas por namespaces e interfaces. Não há serviço privilegiado permanente, telemetria, instalação automática de drivers ou banco de dados.

## Dois caminhos de entrada

**Monitoramento:** notificações `WM_INPUT` fornecem deslocamentos relativos e identidade do dispositivo para o laboratório e medições de eventos. Essas informações não substituem os pacotes que outros aplicativos recebem.

**Correção do cursor:** um `WH_MOUSE_LL` processa deslocamentos em coordenadas de tela e publica posições corrigidas com `SendInput`. O hook usa uma thread com fila de mensagens própria. Entradas injetadas são ignoradas para evitar recursão; cliques e roda continuam pelo caminho original, exceto remapeamentos expressamente selecionados. O cursor é global, sem seleção garantida por mouse.

As unidades e curvas diferem: Raw Input observa contagens do dispositivo; o hook observa movimento já convertido para coordenadas de tela. O laboratório demonstra o mesmo algoritmo, não uma equivalência quantitativa entre esses caminhos.

## Estado e persistência

Perfis contêm valores declarativos; carregar/importar não deve ser confundido com aplicar preferências globais ou ativar processamento. Alterações Windows são confirmadas por releitura e possuem tentativa de rollback. Essa confirmação não é uma confirmação do firmware.

O armazenamento usa JSON limitado a 1 MiB, até 256 perfis, arquivo temporário seguido de substituição e backup. Perfis inválidos são rejeitados. O original corrompido é preservado quando há recuperação por backup; corrupção sem recuperação deve ser informada e não sobrescrita silenciosamente.

O caminho dos dados é por usuário, em `%LOCALAPPDATA%`. Não é necessário executar como administrador. A distribuição portátil mantém o executável separado dos dados do usuário.

## Limites de confiança

HID genérico descreve entrada; não padroniza RGB, DPI, debounce ou lift-off. Adaptadores devem declarar identidade compatível e implementar apenas protocolos documentados. A classificação de capacidade é explícita e comandos não suportados não são enviados. Sem protocolo e dispositivo de teste, esses controles permanecem indisponíveis.

Não há captura de teclas digitadas, upload de dados ou persistência de trajetórias. Diagnósticos devem ser agregados. Laboratório mantém somente uma janela limitada de amostras em memória.
