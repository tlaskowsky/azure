# ラボ ヒント

Azure では、IP アドレスを割り当てて、使用されていない場合にそれを保持することができます。これを _静的 IP アドレス_ と呼び、PIP の属性です。

PIP はポータルまたはコマンドラインで更新できます（[`az network public-ip update`](https://learn.microsoft.com/ja-jp/cli/azure/network/public-ip?view=azure-cli-latest#az-network-public-ip-update) を参照）、しかし、それは VM に使用されていない場合にのみ変更できます。

> もっと必要ですか？こちらが[解決策](solution_jp.md)です。
