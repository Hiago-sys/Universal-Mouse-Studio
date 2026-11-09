# Aprimoramentos 0.1.2

## Movimento

A suavização agora limita a magnitude da saída à magnitude da entrada atual. Antes, uma sequência rápida seguida de um deslocamento de uma contagem podia reaproveitar grande parte da velocidade anterior. A correção evita essa amplificação, mantém suavização independente e não gera movimento após a parada. A correção por tolerância/histerese da revisão 0.1.1 permanece.

Novos testes cobrem desaceleração com suavização de 90%, simetria nos quatro sentidos, a mesma sequência espacial em intervalos de 125 e 8000 eventos/s, 50 mil vetores pseudoaleatórios com saída finita/sem amplificação e desativação durante suavização. Os intervalos são sintetizados: não medem comunicação USB nessas taxas.

## Aplicação e observabilidade

O código matemático é aquecido antes da instalação do hook para reduzir trabalho de inicialização no primeiro callback. Isso não é medição de latência ponta a ponta. Contadores em memória mostram eventos físicos recebidos pelo hook, transformações matemáticas, reinjeções aceitas por SendInput e eventos cujo deslocamento calculado foi zero. Nenhuma trajetória é armazenada por esses contadores. Hook instalado, transformação e reinjeção são estados diferentes; nem uma reinjeção aceita prova efeito em todos os aplicativos.

A interface atualiza esses indicadores a cada 500 ms somente enquanto o hook está em execução. O laboratório continua independente, com contadores próprios. Programas elevados, desktop seguro e jogos com Raw Input ainda exigem validação separada.

## Continuidade e fechamento

Há um botão Minimizar para manter o programa acessível na barra de tarefas com processamento ativo. Fechar com o hook ativo permite escolher minimizar, encerrar ou cancelar. Encerrar remove o hook. Não há execução escondida, inicialização automática ou reativação automática do hook. Também foi corrigido o agendamento do fechamento após salvar: a nova chamada de Close ocorre em uma operação posterior do Dispatcher, evitando reentrada no evento Closing.

## Validação

A suíte contém 35 testes de movimento, 19 de perfis/capacidades/diagnóstico e 12 do controle WPF/lifecycle. O workflow Windows precisa aprová-los antes de publicar a Release. Os ensaios WPF usam entradas sintetizadas e renderização por software; não validam mouse físico nem o hook global. Logs locais adicionais ficam em docs/evidence/improvements-*.log. O benchmark mede apenas o algoritmo em Release, sem comprovar latência do cursor.

A validação de precisão percebida, captura real, recuperação e compatibilidade de aplicações continua necessária no computador do usuário. RGB físico, alteração do sensor e driver não foram adicionados.
