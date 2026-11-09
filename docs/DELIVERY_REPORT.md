# Relatório de entrega — Universal Mouse Studio 0.1.0

Esta é uma entrega executável inicial, com código completo dos recursos descritos abaixo. **O escopo integral solicitado ainda não foi concluído.** Não é apresentada como versão de produção: o ambiente é Linux, e nenhum teste de execução em Windows ou hardware real foi realizado. A ausência de Windows não impede a entrega do código Win32 e do pacote compilado, mas impede validar vários critérios essenciais.

## 1. Estrutura implementada

Solução com cinco projetos: Core, Windows, App WPF e dois executáveis de testes. O Core agrupa movimento, perfis, capacidades e diagnósticos. O Windows isola P/Invoke, HID, Raw Input, configurações e hook. O App contém composição manual com interfaces de serviços, MVVM, comandos, laboratório e navegação em 11 seções. `scripts/` contém build/publicação e fonte de instalador Inno Setup; `native/OptionalInputFilter/` contém estudo e limites, sem driver. O repositório REA anterior foi preservado; não foi necessário usar engenharia reversa para criar esta implementação nova.

## 2. Tecnologias

C#; .NET 10; WPF; Win32/Raw Input/HID/SetupAPI/Configuration Manager; JSON local. Sem biblioteca de UI, banco de dados ou pacote NuGet externo. Packs Microsoft oficiais são necessários no restore. DI manual por interfaces para os serviços de configurações e processamento, sem contêiner externo.

## 3. Funcionalidades concluídas e verificadas aqui

Motor matemático determinístico com intensidade, eixos, tolerância, diagonais, suavização independente, detecção por distância, histerese, reinício após pausa e quantização residual. Testes cobrem correção efetiva e passthrough fora da tolerância. Perfis com validação, round-trip, importação/exportação limitada, identificadores independentes e recuperação de corrupção. Classificação de capacidades e recusa de RGB sem adaptador. Estimativas estatísticas e contagem de intervalos curtos de clique. Tudo isso foi executado em testes portáveis.

## 4. Implementado, mas dependente de validação Windows

- Interface WPF: dashboard, dispositivos, sensibilidade/DPI, snapping/laboratório, iluminação com indisponibilidade explícita, botões, scroll/cliques, performance, perfis, diagnóstico e configurações.
- Enumeração com metadados disponíveis, múltiplos dispositivos e notificações de conexão. Seleção de dispositivo filtra medições, não ajustes globais.
- Configuração real de velocidade, aceleração, inversão principal, clique duplo, ClickLock, rolagem por linhas/página e janelas inativas. Aplicação fora da thread visual, confirmação por leitura e rollback verificado quando possível.
- Hook opcional dedicado com reinjeção `SendInput`, emergência Ctrl+Alt+F11, suspensão de sessão/display, eventos injetados ignorados e falha que deixa a entrada original passar.
- X1/X2 por software com copiar/colar/recortar/desfazer, volume e play/pause. Não persiste na memória do mouse; mapeamento desta sessão não integra o arquivo de perfil.
- Perfis da interface, associação ao executável e troca opcional com ajustes globais. Tema e autorização de troca automática não são persistidos.
- Laboratório limitado a 3000 amostras em memória, comparação e reprodução sem mover cursor. Teste de frequência de 10 segundos, calibração aproximada por contagens, relatórios agregados e teste de cliques/rolagem.
- Registros técnicos locais limitados com componente/código de erro; sem mensagens de exceção, trajetórias ou teclas. Nenhuma telemetria.

O funcionamento final dessas integrações e o aspecto visual não foram confirmados por execução Windows. A compilação não substitui essa confirmação.

## 5. Funcionalidades indisponíveis / não implementadas

Não há adaptador físico de RGB, DPI, polling, debounce, lift-off distance ou Motion Sync. Sem dispositivo/protocolo conhecido não é possível determinar se o limite é físico, de firmware ou apenas falta de integração. Não foram enviados comandos HID desconhecidos. Efeitos RGB e sincronização física permanecem indisponíveis.

Driver de filtro não foi implementado; não há `.sys`, INF experimental, instalação automática ou alteração de mecanismos de segurança. O estudo documenta por que o exemplo oficial Moufiltr não é uma solução HID universal pronta. Inversão de scroll, remapeamento principal completo, botão DPI, abertura de programas por botão, idiomas adicionais, atualizador automático e instalador assinado não estão implementados. Existe somente a estrutura de instalador Inno Setup, sem compilação dele.

## 6. Estado real do Angle Snapping

**PARCIALMENTE IMPLEMENTADO conforme os critérios de aceitação do pedido.** O algoritmo está implementado e testado; o laboratório e o mecanismo real de cursor estão no código e na build. Falta executar no Windows e demonstrar resultado/recuperação em aplicativos reais. O laboratório usa contagens Raw Input; o hook usa deslocamentos em coordenadas de tela já transformadas pelo Windows. Esses espaços não são equivalentes.

A correção não é universal para jogos, Raw Input, desktop seguro ou programas elevados. O hook não identifica a origem física do evento e afeta o cursor agregado de todos os mouses. Não há contorno de anticheat. O componente perpendicular removido pelo snapping não é devolvido mais tarde; apenas frações de arredondamento são preservadas. O desligamento é por Ctrl+Alt+F11 ou botão; o fechamento remove o hook. Tudo isso ainda precisa do ensaio Windows documentado.

## 7. Testes e desempenho

**43 testes automatizados aprovados:** 24 de movimento/coordenadas/quantização e 19 de perfis/capacidades/diagnósticos. Executados com .NET 10.0.14 em Linux x64. Logs e comandos em `BUILD_RESULTS.md` e `evidence/`.

Benchmark observado: 2 milhões de eventos em 0,4395774 s, média de **219,7887 ns/evento**, **0 bytes** alocados no trecho medido. É somente o algoritmo, não a latência física, do hook ou do cursor. Não houve medição ponta a ponta em mouse real, RAM da interface, renderização ou performance de Windows modesto.

## 8. Compilação e distribuição

Solução Release compilada sem erros nem avisos. Publicação self-contained `win-x64` concluída. O executável foi identificado como PE32+ GUI x86-64; esse exame não demonstra que ele abriu no Windows. O pacote contém runtime desktop e deve ser extraído integralmente. Executável não assinado. Arquivo-fonte acompanha solução, testes, scripts, documentos e logs, sem SDK/cache/builds.

## 9. Problemas conhecidos e limites

Principal pendência: validar P/Invoke, renderização WPF, troca de perfis, restauração e entrada real em Windows 10/11. Configurações globais podem conflitar com software do fabricante ou mudanças externas. Monitores com DPI misto, lacunas entre telas, clipping, desktop seguro, UIPI, hooks de terceiros e suspensão são casos obrigatórios de ensaio. Windows pode remover silenciosamente um hook lento; o indicador representa o estado conhecido do serviço, não uma garantia independente de que o Windows continua entregando callbacks. A falha de limpeza é registrada e bloqueia novo processamento quando o handle permanece incerto.

Estimativas de polling são frequência de eventos recebidos, não USB. Intervalos curtos não diagnosticam defeito físico. Calibração soma o caminho e é afetada por ruído/desvios. Nem todas as informações de fabricante/conexão são expostas pelo dispositivo; receptores USB não provam qual rádio está sendo usado. A interface não foi auditada visualmente nem por leitor de tela.

## 10. Executar, instalar e remover

Extraia o ZIP portátil inteiro e abra `UniversalMouseStudio.exe` em Windows 10/11 x64, sem administrador. .NET vem no pacote. Mantenha o hook desligado até ler `USER_GUIDE.md` e seguir `TESTING.md`. O laboratório pode demonstrar a matemática sem ativar correção global.

Perfis ficam em `%LOCALAPPDATA%\UniversalMouseStudio`. Ajustes globais aplicados persistem após fechar, como no Painel de Controle; use Restaurar antes de fechar se quiser revertê-los. Para remover, feche o aplicativo e apague a pasta portátil; apague os dados locais apenas se quiser remover perfis/logs. Para recompilar, instale SDK .NET 10 e execute `scripts/build.ps1` e `scripts/publish.ps1` no Windows. O instalador opcional requer Inno Setup instalado à parte e ainda não foi compilado.

## 11. Próximas melhorias e matriz de aceitação

Prioridade: roteiro Windows com gravação de resultados, correção dos defeitos encontrados, teste em dispositivos básicos/Bluetooth/receptores e medição ponta a ponta. Depois: revisão visual/acessibilidade, persistência das opções restantes, protocolos de hardware documentados escolhidos com dispositivos disponíveis, empacotamento/assinatura e pipeline Windows CI. Driver só merece implementação com escopo de pilha definido, WDK, recuperação e processo de assinatura legítimo.

A matriz completa com os quatro estados exigidos está em `STATUS.md`. Ela registra o motor/perfis como **FUNCIONAL E TESTADO**, integrações como **IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE**, aceitação final do snapping como **PARCIALMENTE IMPLEMENTADO** e RGB físico/driver como **NÃO IMPLEMENTADO**. Não há declaração de conclusão integral ou de compatibilidade universal.
