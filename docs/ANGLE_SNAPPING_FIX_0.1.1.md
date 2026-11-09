# Correção de Angle Snapping 0.1.1

## Problema reproduzido

O usuário relatou efeito ausente no laboratório, mesmo com o hook global ativo. Esses modos são independentes, e o indicador do hook não confirma que a captura do laboratório está ocorrendo.

Além dessa ambiguidade, foi reproduzida uma falha matemática: em uma sequência sintetizada de 640 pacotes com dx=1 e dy=1 a cada oito eventos, a trajetória média tem 7,1 graus. Apesar de estar na tolerância padrão de 8 graus, o pacote individual com dy=1 mede 45 graus e soltava o eixo. A versão anterior corrigiu **0 de 80** desses desvios. Esse resultado foi observado antes de modificar o motor e consta no log de regressão.

## Alterações

- A direção acumulada já estabelecida admite componentes perpendiculares isoladas de até uma contagem, dentro do cone de histerese. Até duas contagens líquidas consecutivas podem ser tratadas como ruído antes de liberar uma virada deliberada. Não há garantia de preservar uma virada ambígua menor que esse limite.
- Uma componente perpendicular pequena não é confundida automaticamente com uma reversão do movimento.
- O quantizador conserva frações entre eventos alinhados dentro do mesmo lock, sem aplicá-las a eventos identidade. Saiu do eixo/pausou/configurou/desativou: o estado é limpo.
- O laboratório mostra amostras recebidas, eventos corrigidos e posição bruta/corrigida.
- Seleção explícita do mouse pelo próximo movimento inicia a captura no dispositivo correto. Não ativa o hook.
- Ajustes recalculam as amostras já retidas; o gráfico não mantém silenciosamente o resultado da configuração anterior.
- Estado pausado e limite de 3000 amostras ficam explícitos. Reiniciar a captura após o limite limpa o teste anterior.
- Presets redefinem forças por eixo e distância de detecção para evitar um preset Forte com eixos em intensidade zero por configuração anterior.

## Evidência de testes executados

Cross-build Release em Linux: **0 erros, 0 avisos**. Suíte de movimento: **30/30**. Suíte de perfis/capacidades/diagnósticos: **19/19**. Total **49 testes**. Os cinco testes adicionais e o caso de pacotes de quatro contagens cobrem regressões reais da nova interpretação; testes existentes de curvas, movimentos fora da tolerância, reversão e interrupção continuam aprovados.

Na sequência sintetizada que falhava, foram observados **80 eventos corrigidos**, com deslocamento vertical bruto **80** e corrigido **28**, usando intensidade padrão de 65%. Isso mede transformação matemática; não comprova a interface ou um mouse físico. A mesma revisão passa pelos scripts de build/testes do workflow Windows antes da publicação da Release.

Logs em `docs/evidence/angle-*`. O benchmark da versão 0.1.0 é histórico e não foi repetido para esta correção, pois o objetivo é corrigir comportamento, não reivindicar melhora de desempenho.

## Como conferir no computador do usuário

1. Fechar a versão antiga e extrair a Release 0.1.1 em uma pasta nova. A janela identifica 0.1.1.
2. Deixar o hook desligado para isolar o laboratório; o laboratório não precisa dele.
3. Escolher Forte e deixar Aplicar algoritmo no laboratório marcado.
4. Clicar Escolher mouse pelo movimento e mover o mouse que será testado.
5. Fazer um movimento aproximadamente horizontal ou vertical com pequenos desvios. Conferir amostras e corrigidas; movimentos perfeitamente retos não precisam de correção.
6. Cinza representa o caminho bruto e verde o corrigido. Comparar com a opção do algoritmo desligada; mudar a opção recalcula as mesmas amostras.
7. Se amostras=0, a entrada não foi capturada. Se há amostras mas corrigidas=0, verificar configurações e o tipo de trajetória. Esses estados não significam sucesso.

## Limites ainda abertos

Não foi executado ensaio interativo em Windows/mouse físico neste ambiente. A integração de cursor permanece experimental, e Raw Input de jogos não é substituído pelo hook. O relato do usuário não deve ser marcado como resolvido em seu computador apenas pelo teste sintetizado. O novo pacote oferece uma correção comprovada do caso reproduzido e meios mais claros de verificar o caso real.


## Verificação adicional do controle WPF no Windows

`tests/UniversalMouse.WpfTests` carrega o XAML e o controle real, alimenta pacotes sintetizados, confere estatísticas/reprocessamento, renderiza as trajetórias em bitmap e verifica pixels das duas cores. Também cobre desligamento do algoritmo, pausa, reprodução, limpeza e limite de amostras. Não instala hook nem simula mouse físico. O build Windows executa esses nove testes antes de publicar; uma falha impede a Release. A imagem gerada é publicada como `wpf-motion-lab.png`. Essa imagem é uma renderização automatizada do controle com dados sintetizados, não captura da tela ou mouse do usuário.
