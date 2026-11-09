# Guia do usuário

## Executar

Extraia o pacote portátil em uma pasta de sua escolha e abra `UniversalMouseStudio.exe` no Windows 10/11 x64. Não execute como administrador. A distribuição autocontida inclui o runtime; uma publicação dependente do framework exige **.NET 10 Desktop Runtime x64**. Não há driver adicional. O executável ainda não tem assinatura comercial; esta versão precisa de validação no Windows antes de distribuição ampla.

O aplicativo inicia com a correção do cursor desligada. Conecte seu mouse, abra **Dispositivos** e confira o que o Windows informou. Selecionar um dispositivo filtra o laboratório; o cursor do Windows e suas preferências permanecem globais.

## Ajustes comuns

Em **Sensibilidade e DPI**, altere a velocidade do ponteiro e a precisão aprimorada e use **Aplicar ajustes ao Windows**. **Reler valores reais** atualiza os controles. A velocidade 1–20 não é o DPI do sensor. A calibração por distância fornece uma estimativa de contagens por polegada, nunca uma leitura do DPI físico. Faça o movimento com régua, usando somente o dispositivo selecionado; os resultados dependem da amostragem e do caminho percorrido.

**Scroll e cliques** permite ajustar preferências que o Windows oferece: linhas/página, rolagem de janelas inativas, intervalo de clique duplo e ClickLock. A inversão de botões principais é uma preferência do sistema. Valores são aplicados ao Windows somente pelo comando correspondente; editar um perfil não significa aplicar ao sistema.

Para desfazer preferências persistentes, use **Restaurar valores da abertura** antes de fechar. Esse baseline é o estado capturado durante esta execução, não uma configuração de fábrica. Alterações feitas simultaneamente por outro programa podem ser substituídas ao restaurar. Encerrar o aplicativo desliga o processamento, mas não desfaz automaticamente preferências globais aplicadas explicitamente.

## Angle Snapping e laboratório

1. Abra **Angle Snapping**. Comece com perfil leve, sem diagonal nem suavização.
2. Inicie **Capturar / pausar** no laboratório e mova o mouse. A linha cinza representa deslocamento bruto; a verde, o resultado do algoritmo.
3. Ajuste intensidade e tolerância. Movimentos fora da tolerância devem preservar mudanças intencionais. A histerese define uma margem de saída após adquirir uma direção.
4. **Repetir teste** processa novamente somente as amostras limitadas mantidas em memória. Não movimenta o cursor. **Limpar** descarta o desenho e as amostras.
5. Para testar o cursor do Windows, use **Ativar correção do cursor**. O estado global é exibido separadamente do laboratório.
6. Use **Desativar imediatamente** ou **Ctrl+Alt+F11** para interromper o hook.

O laboratório usa contagens Raw Input; o cursor é corrigido em coordenadas de tela após transformações do Windows. As linhas do laboratório não comprovam que um jogo ou outro programa recebeu movimento corrigido. Não há garantia para Raw Input, programas elevados, desktop seguro ou anticheat. Faça primeiro os testes de recuperação de `TESTING.md` numa sessão não crítica.

## Botões

Botões expostos por Raw Input podem ser observados no teste. Inversão de botões principais é aplicada pelo Windows. Remapeamentos laterais, quando selecionados, usam eventos de software; não alteram a memória do mouse e dependem da sessão/aplicativo aceitar entrada injetada. Botões proprietários que não produzem eventos conhecidos não são automaticamente configuráveis. O atalho de emergência continua disponível enquanto o serviço está ativo.

## Perfis

A seleção de perfil carrega os controles. Use **Aplicar perfil** para mudar ajustes globais. É possível criar, duplicar, renomear, excluir, importar e exportar JSON; o armazenamento fica em `%LOCALAPPDATA%\UniversalMouseStudio` e possui backup. Nomes e conteúdo importado são validados. Antes de restaurar os perfis iniciais, exporte os que deseja conservar.

Para associar um perfil a um aplicativo, informe somente o nome do executável, por exemplo `notepad.exe`, e ative a opção de troca automática. Essa opção pode alterar preferências globais ao trocar a janela ativa e mantém o último perfil ao sair do aplicativo associado. Não é isolamento por processo e nunca deve ligar o hook por conta própria. A automação inicia desativada a cada execução.

## Performance e diagnóstico

Faça o teste de frequência movendo continuamente o mouse por dez segundos. A frequência é a estimativa de chegada de eventos ao programa; não é um analisador do barramento USB e não comprova a taxa física declarada pelo fabricante. Eventos de múltiplos mouses e pausas podem alterar a medição.

Testes de cliques contam eventos na sessão de teste. Intervalos curtos são apenas candidatos a duplicação; não comprovam defeito do switch. Teste scroll e movimento separadamente. Um estado inconclusivo é esperado quando faltam amostras.

O relatório técnico JSON contém agregados e ajustes; não contém trajetórias, teclas digitadas ou identificadores persistentes. Revise o relatório antes de compartilhar.

## Iluminação e funções físicas

A página **Iluminação** explica quando é necessária uma integração específica. Esta versão não envia comandos RGB sem adaptador documentado. Cores, DPI físico, polling físico, LOD, Motion Sync e debounce de firmware não são universais e não são simulados como aplicados.

## Privacidade, temas e remoção

A captura do laboratório é limitada e em memória; o programa não registra trajetórias permanentemente nem grava teclas. Não há telemetria ou atualizador automático. O tema claro fica em **Configurações**.

Para remover a versão portátil: desative a correção, restaure preferências Windows se desejar, feche o aplicativo e remova a pasta extraída. Para apagar também os perfis, remova `%LOCALAPPDATA%\UniversalMouseStudio` depois de exportar os dados desejados. Não há serviço ou driver a desinstalar.
