# Filtro opcional — somente estudo de arquitetura

Este diretório não contém um driver pronto. Não há `.sys`, INF, binário de instalação ou instrução para desativar a assinatura de drivers. O aplicativo não depende de componente de kernel.

A investigação do exemplo oficial Microsoft WDF Moufiltr e os critérios para uma implementação futura estão em [`docs/DRIVER_REQUIREMENTS.md`](../../docs/DRIVER_REQUIREMENTS.md). Seu INF de exemplo é PS/2, e a parte de ISR é específica de `i8042prt`; não representa suporte universal USB/Bluetooth.

O caminho futuro deve transformar apenas pacotes relativos autorizados, preservar campos de botões/roda e passar entradas desconhecidas intactas. A transformação matemática precisa ser portada para código adequado ao kernel e submetida a ensaios de PnP, energia, remoção, carga, segurança e recuperação. Nenhum artefato de driver não validado é distribuído nesta versão.
