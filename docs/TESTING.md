# Testes e critérios de validação

## Ambiente e distinção de evidências

A implementação inicial usa Linux com SDK .NET 10 para compilar bibliotecas portáveis, executar testes do Core e gerar artefatos Windows com targeting habilitado. Isso não executa WPF, não abre dispositivos HID Windows e não valida `SendInput` ou preferências Win32. Os resultados efetivamente medidos devem ser registrados no relatório de entrega; os itens abaixo não devem ser considerados aprovados apenas por estarem documentados.

## Testes automatizados portáveis

Os projetos de teste são executáveis de console sem framework externo. Usar `dotnet run`, e não interpretar uma saída vazia de `dotnet test` como execução dos testes:

```powershell
dotnet run --project tests/UniversalMouse.Tests -c Release
dotnet run --project tests/UniversalMouse.ProfileTests -c Release
```

A suíte do motor cobre movimento horizontal, vertical, diagonal, desvios pequenos e grandes, curvas, velocidades, movimentos curtos, mudanças de direção, pausas, ativação/desativação e entradas inválidas. Testes de quantização verificam a conservação do resto de arredondamento, não a conservação do componente removido intencionalmente pelo snapping. A suíte de perfis cobre validação, importação, exportação e corrupção/recuperação; capacidades e métricas têm ensaios com entradas controladas. Verifique a saída dos executáveis para a lista realmente executada.

O benchmark usa `dotnet run --project tests/UniversalMouse.Tests -c Release -- --benchmark` e deve ser executado em Release, após aquecimento, sem depurador. Informe CPU, ambiente, quantidade de eventos e tempo medido. Custo do método não equivale a latência ponta a ponta de mouse/cursor nem a custo de hook/Win32.

## Roteiro obrigatório no Windows — pendente

| Teste | Procedimento e resultado esperado |
| --- | --- |
| Inicialização | Executar como usuário comum em Windows 10 e 11; navegar por teclado, escalar DPI, trocar tema; sem exceções ou dependência de privilégio |
| Identificação | Comparar dispositivos com Gerenciador de Dispositivos; campos ausentes ficam não informados; testar USB, receptor e Bluetooth disponíveis |
| Hotplug | Conectar/remover e suspender/retomar; interface atualiza sem manter handles inválidos |
| Preferências | Registrar baseline externo; alterar um valor, reler no sistema e restaurar; testar falha parcial e exceção de rollback |
| Cursor | Ativar correção numa área de desenho externa; ensaiar trajetórias próximas e fora da tolerância e intensidade zero |
| Emergência | Pressionar Ctrl+Alt+F11 durante movimento; correção cessa imediatamente, cliques e roda continuam |
| Encerramento | Desligar, fechar e encerrar abruptamente o processo em sessão descartável; cursor continua utilizável; preferências persistentes são verificadas separadamente |
| Reinjeção | Monitorar eventos injetados; confirmar ausência de ciclo, duplicação de movimento ou repressão de cliques/roda |
| Multimonitor | Testar origens negativas, bordas, monitores com escalas distintas, captura e clipping; nenhum salto para outra tela |
| Pausa e retorno | Mover, parar, inverter direção e alternar opções; sem cauda de movimento ou restos inesperados |
| Raw Input | Comparar aplicativo de cursor comum com consumidor Raw Input; registrar por aplicativo a diferença e não generalizar |
| Integridade | Testar consumidor elevado sem elevar o Studio; falha de injeção precisa liberar entrada original e informar erro |
| Remapeamento | Se implementado, verificar down/up de X1/X2, modificadores, repetição, falha de injeção e restauração do comportamento original |
| Perfis | Criar, duplicar, renomear, excluir, importar/exportar, reiniciar; valores persistem sem ativar movimento sem intenção |
| Aplicativos | Se troca automática estiver implementada, testar associação explícita, saída de aplicativo e conflitos com alterações globais |
| Corrupção | Arquivo truncado, campos inválidos, tamanho excessivo e permissão negada; sem sobrescrita silenciosa de dados |
| Desempenho | Testar 125/500/1000 Hz e taxas superiores disponíveis, uso de CPU/RAM e limites da interface; medir com ferramenta externa apropriada |
| Diagnóstico | Comparar eventos e relatório; estimativas não afirmam falha física definitiva nem polling USB preciso |

## Critérios de aceitação do Angle Snapping

Os testes matemáticos e uma compilação não bastam. É preciso demonstrar a correção no laboratório, aplicá-la ao cursor do Windows, confirmar ativação/desativação/recuperação e registrar as limitações por consumidor. Até esses ensaios, o mecanismo Win32 permanece **IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE**.

Ensaios físicos de DPI, iluminação, firmware e polling dependem de dispositivo/protocolo compatível. Não foram executados no ambiente Linux.
