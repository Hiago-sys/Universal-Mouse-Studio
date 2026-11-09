# Compatibilidade

O executável tem como alvo Windows 10/11 x64 e o runtime .NET 10 Desktop. A edição autocontida inclui o runtime. A primeira entrega foi compilada em Linux; testes físicos no Windows continuam necessários.

| Recurso | Origem real | Limite |
| --- | --- | --- |
| Velocidade do ponteiro, aceleração, inversão de botões, clique duplo, ClickLock, linhas de rolagem | Preferências globais Win32 | Afetam o sistema; não alteram DPI físico e podem ser ignoradas por aplicativos Raw Input |
| Enumeração e eventos | Raw Input, interfaces HID disponíveis | Informações variam por dispositivo, receptor e permissões |
| Correção angular do cursor | Hook e reinjeção em modo usuário | Não equivale a filtrar pacotes HID; compatibilidade por aplicativo |
| Laboratório de movimento | Transformação matemática dos eventos recebidos | Visualização não comprova aplicação a outro aplicativo |
| Estimativa de frequência | Intervalos entre eventos recebidos | Não mede com precisão o polling USB; carga, coalescência, agendamento e movimento influenciam |
| Perfis | Configuração local JSON | Aplicação de propriedades globais não oferece isolamento por aplicativo |
| DPI/polling físicos, LOD, Motion Sync, debounce de firmware | Protocolo de hardware | Não implementados sem adaptador documentado |
| RGB | Protocolo do fabricante/dispositivo | Arquitetura extensível; nenhum controle físico universal presumido |

## Classificação das capacidades

1. **Nativo do Windows:** API documentada do sistema.
2. **Software:** transformação feita no aplicativo.
3. **Hardware:** comando conhecido e verificado em adaptador específico.
4. **Integração específica:** exige provedor/protocolo adicional conhecido.
5. **Não suportado:** incompatibilidade estabelecida.
6. **Desconhecido:** faltam evidências. Ausência de adaptador não prova impossibilidade física.

Um mouse básico, RGB autônomo ou receptor sem fio pode não expor comandos configuráveis. Fabricante, modelo, transporte, botões e identificadores podem estar incompletos. Campos ausentes devem ser apresentados como não informados; nomes comerciais não são inferidos de VID/PID sem base confiável. Um receptor pode representar vários dispositivos; Bluetooth pode aparecer por interfaces intermediárias. Múltiplos mouses podem ser monitorados separadamente, mas o processamento de cursor via hook é global.

## Aplicativos, jogos e segurança

`SendInput` respeita UIPI: um processo comum não pode injetar em um processo de integridade superior. Área de trabalho segura, telas de credenciais, sessões remotas, clipping e aplicativos que reposicionam o cursor exigem verificação própria. Aplicativos que usam Raw Input podem continuar recebendo movimento sem correção. Não há integração nem garantia de compatibilidade com anticheats. O programa não eleva privilégios para contornar essas restrições.

Evite ativar a correção em tarefas críticas antes de executar a lista manual de testes. A tecla de emergência é **Ctrl+Alt+F11**. Encerrar o processo remove o hook; preferências Windows aplicadas explicitamente podem persistir e devem ser restauradas separadamente.
