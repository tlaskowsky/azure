# ラボの解決策

**この UX は使い勝手が良くありません。**

ポータルで App Service を開き、_診断 & 問題解決_ を選択し、次に _診断ツール_ に進みます。

_Auto-Heal_ をクリックし、それを有効にしてルールを追加します：

- 1つのリクエストがステータスコード 500 を持つ場合
- 30秒のウィンドウ内で
- プロセスをリサイクルします。

設定を保存し、ランダム数を複数回呼び出してアプリを壊します。両方のインスタンスが失敗したときに /healthz エンドポイントをチェックすると、数分以内に新しいインスタンスに置き換えられるのがわかります。

以下は、失敗したインスタンスが置き換えられていることを示す私の出力です：



```
PS>curl https://rng-api-es2.azurewebsites.net/healthz
{"message":"Instance: f08cd58fd50f. Unhealthy"}
PS>curl https://rng-api-es2.azurewebsites.net/healthz
{"message":"Instance: 325b6b19d360. Unhealthy"}
PS>curl https://rng-api-es2.azurewebsites.net/healthz
Instance: c45fb7b5cd08. Ok
PS>curl https://rng-api-es2.azurewebsites.net/healthz
Instance: c45fb7b5cd08. Ok
PS>curl https://rng-api-es2.azurewebsites.net/healthz
Instance: a6a7a06dc0d1. Ok
```
