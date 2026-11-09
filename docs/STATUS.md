# Matriz de implementação e aceitação

Esta matriz descreve a primeira entrega e deve ser lida junto de `BUILD_RESULTS.md`, que registra os comandos realmente executados. **Compilação para Windows não é teste de execução no Windows.** O ambiente de desenvolvimento é Linux; nenhum mouse conectado ao Windows foi validado aqui.

Estados obrigatórios:

- **FUNCIONAL E TESTADO:** execução automatizada portável confirmada; não implica ensaio físico.
- **IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE:** código de integração disponível, sem execução Windows/hardware comprovada.
- **PARCIALMENTE IMPLEMENTADO:** parte da função existe, com critérios de aceitação pendentes.
- **NÃO IMPLEMENTADO:** nenhum controle operacional correspondente é oferecido.

| Área / recurso | Estado | Evidência e limite |
| --- | --- | --- |
| Motor determinístico de snapping | FUNCIONAL E TESTADO | Suíte portável do motor: 30 testes aprovados; não comprova correção do cursor Windows |
| Intensidade, tolerância, eixos, diagonal, histerese e suavização | FUNCIONAL E TESTADO | Configurações do motor e validação; integração da interface depende de Windows |
| Quantização fracionária | FUNCIONAL E TESTADO | Não devolve componente intencionalmente removido pela correção |
| Perfis JSON, validação, importação/exportação e recuperação | FUNCIONAL E TESTADO | Suíte portável de perfis/capacidades/diagnósticos: 19 testes aprovados; permissões reais do usuário ainda exigem teste |
| Classificação de capacidades e bloqueio de funções sem adaptador | FUNCIONAL E TESTADO | Testes de contrato; nenhum protocolo físico validado |
| Estimador de frequência e detector de intervalos de clique | FUNCIONAL E TESTADO | Entradas controladas; não é teste físico de USB ou switch |
| WPF/MVVM, navegação, temas e comandos | IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE | Cross-build não abre a interface gráfica |
| Enumeração, identidade HID e hotplug | IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE | Win32 implementado; matriz USB/Bluetooth/receptor pendente |
| Preferências Windows e restauração | IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE | Leitura, aplicação e tentativa de rollback em código; ensaio Windows pendente |
| Hook, reinjeção e atalho de emergência | IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE | Modo usuário; falta confirmação de segurança e resultado em sessão Windows |
| Laboratório de movimento e reprodução local | IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE | Interface e motor; captura/display precisam de Windows |
| Angle Snapping como recurso final completo | PARCIALMENTE IMPLEMENTADO | Critérios de aplicação, recuperação e Raw Input no Windows ainda abertos |
| Remapeamento de botões adicionais | PARCIALMENTE IMPLEMENTADO | Conferir subconjunto de ações no aplicativo; sem armazenamento no hardware |
| Perfis por aplicativo | PARCIALMENTE IMPLEMENTADO | Associação e troca global opcional; isolamento por processo inexistente |
| Calibração por distância | PARCIALMENTE IMPLEMENTADO | Estimativa por contagens; não mede configuração física do sensor |
| Relatórios/diagnóstico integrados e medição real de eventos | IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE | Não produz diagnóstico físico definitivo |
| RGB físico e efeitos | NÃO IMPLEMENTADO | Interfaces extensíveis; nenhum adaptador documentado/testado |
| DPI físico, polling físico, debounce, LOD e Motion Sync | NÃO IMPLEMENTADO | Dependem de integração de hardware conhecida |
| Inversão de scroll por software | NÃO IMPLEMENTADO | Sem comando aparente que finja aplicação |
| Driver opcional assinado | NÃO IMPLEMENTADO | Estudo Moufiltr e requisitos; nenhum `.sys`/INF distribuído |
| Publicação Windows x64 portátil | IMPLEMENTADO, MAS NÃO VALIDADO EM HARDWARE | Executável PE x64 e runtime publicados; execução Windows pendente |
| Instalador comercial assinado e atualizações seguras | NÃO IMPLEMENTADO | Estrutura de empacotamento não equivale a assinatura ou serviço de atualização |

## Bloqueios para aceitação completa

1. Executar o roteiro manual em Windows 10 e 11, com máquina recuperável.
2. Validar o serviço de entrada sob carga, falhas de `SendInput`, hotplug, suspensão, encerramento, clipping e multimonitor.
3. Medir custo e latência ponta a ponta em hardware; números do algoritmo isolado não substituem essa medição.
4. Ensaiar Raw Input e consumidores por aplicativo, sem generalizar a jogos.
5. Escolher dispositivos com protocolos documentados antes de implementar adaptadores físicos RGB/sensor.
6. Revisar acessibilidade visual, escalas de DPI e navegação por teclado no Windows.
7. Assinar executáveis/instalador e estabelecer processo de publicação antes de distribuição ampla.

O projeto não usa a expressão “100% funcional” e não considera a fase de snapping concluída por haver somente algoritmo e código de hook.


Revisão 0.1.1: correção de pacotes inteiros lentos, seleção do dispositivo pelo movimento, contagem de eventos corrigidos e reprocessamento do laboratório. São 30 testes do motor e 19 portáveis adicionais; ensaio em hardware continua pendente. A versão anterior foi relatada pelo usuário como sem efeito aparente no laboratório, e um caso equivalente foi reproduzido automaticamente.
