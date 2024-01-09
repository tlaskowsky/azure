# ラボの解決策

ここに更新されたJSONファイルがあります：

- [solution/configurable-secret.json](/labs/aks-keyvault/solution/configurable-secret.json)

これを使用して、KeyVaultシークレットの新しいバージョンを設定します：



```
az keyvault secret set --name configurable-secrets --file labs/aks-keyvault/solution/configurable-secret.json --vault-name <kv-name>
```


そして、変更が現在のバージョンとして設定されていることを確認します：


```
az keyvault secret show --name configurable-secrets --vault-name <kv-name>
```


今、アプリをリフレッシュしても変更は表示されません。

ファイルシステムをチェックします：



```
kubectl exec deploy/configurable -- cat /app/secrets/secret.json
```


これも変更を表示しません。[自動ローテーション機能](https://secrets-store-csi-driver.sigs.k8s.io/topics/secret-auto-rotation.html)は存在しますが、デフォルトでは有効になっていないため、シークレットの詳細はPodが作成されたときにのみ読み込まれます。

ロールアウトを強制して、新しいPodで置き換えます：



```
kubectl rollout restart deploy/configurable
```


新しいPodがオンラインになると、アプリは更新された設定を持ちます。
