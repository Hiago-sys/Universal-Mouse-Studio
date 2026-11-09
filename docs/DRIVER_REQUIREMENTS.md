# Processamento avançado e driver opcional

**Não há driver implementado, compilado, assinado ou instalado nesta entrega.** O diretório `native/OptionalInputFilter` contém somente documentação de projeto. O modo usuário funciona independentemente dele. Nenhuma ferramenta altera o modo de assinatura do Windows ou instala componentes experimentais.

## O que o Moufiltr realmente oferece

O exemplo oficial [Mouse Input WDF Filter Driver (Moufiltr)](https://github.com/microsoft/Windows-driver-samples/tree/main/input/moufiltr) é um ponto de partida KMDF para um filtro de mouse específico. O README oficial informa que o exemplo acompanha a cadeia de pacotes e a ISR sem transformar os dados. A interceptação da ISR existe somente na pilha `i8042prt`. O INF fornecido foi feito para **mouse PS/2**.

Isso não estabelece um instalador universal para mouses USB ou Bluetooth. Embora o mecanismo de conexão à classe seja relevante ao estudo da pilha, transporte, posição do filtro, ciclo de vida PnP/power, múltiplos dispositivos e coexistência com filtros de fabricantes exigem um projeto específico. Não se deve copiar a seção PS/2 de INF para todas as instâncias HID ou adicionar filtros globais à classe sem ensaios e recuperação documentados.

Referências Microsoft:

- [Moufiltr sample](https://learn.microsoft.com/en-us/samples/microsoft/windows-driver-samples/mouse-input-wdf-filter-driver-moufiltr/).
- [Mouse and keyboard HID client drivers](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/keyboard-and-mouse-hid-client-drivers).
- [MouseClassServiceCallback](https://learn.microsoft.com/en-us/previous-versions/ff542394(v=vs.85)).
- [Driver signing](https://learn.microsoft.com/en-us/windows-hardware/drivers/install/driver-signing).

## Requisitos antes de uma implementação distribuível

1. Definir Windows e WDK suportados, transporte, IDs das instâncias aceitas e ponto documentado de filtragem.
2. Adaptar o algoritmo para operação determinística e limitada em contexto de callback, sem alocações/paginação inadequadas, chamadas de interface ou esperas bloqueantes. O código C# atual não pode ser executado diretamente no kernel.
3. Preservar botões, roda, flags, pacotes absolutos, identificação de unidade e semântica de consumo de pacotes. Processar somente movimentos relativos conhecidos; passar casos desconhecidos intactos.
4. Definir sincronização, limites e validação de uma interface de controle, com ACL de mínimo privilégio. Estado inicial e falhas devem manter passthrough.
5. Garantir descarregamento e suspensão/retomada seguros, remoção surpresa e funcionamento com múltiplos dispositivos/filtros.
6. Testar em máquina descartável com acesso remoto ou teclado independente e plano de recuperação. Usar Driver Verifier e testes HLK aplicáveis; medir desempenho da pilha, não só o algoritmo.
7. Distribuir somente pacotes com assinatura válida e processo de assinatura/certificação aplicável à versão do Windows. Não é aceitável desativar Secure Boot, integridade de código ou exigir drivers vulneráveis.
8. Criar instalação explícita com elevação apenas nessa etapa, consentimento, registro da instância alvo, rollback e desinstalação. Um erro não deve exigir um mouse funcional para recuperar a máquina.

Não há promessa de compatibilidade com todos os jogos, nem trabalho de contorno de anticheat. Um driver assinado também não garante aceitação por aplicativos protegidos. A engenharia necessária para esse nível é distinta do modo usuário e não está escondida atrás de um controle aparentemente operacional.
