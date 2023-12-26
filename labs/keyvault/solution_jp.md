# ラボの解決策

証明書を作成するためのヘルプテキストを表示します:



```
az keyvault certificate create --help
```


証明書ポリシーを提供する必要があります - すべてのボールトはデフォルトポリシーで作成されます。それをJSONファイルとして保存し、テンプレートとして使用できます:


```
az keyvault certificate get-default-policy > labs/keyvault/lab/default-policy.json
```


以下のファイルにデフォルトポリシーを編集し、私たちが望む証明書の値を設定しました:

- [lab/lab-policy.json](/labs/keyvault/lab/lab-policy.json)

これで、そのカスタムポリシーを使用して証明書を作成できます:



```
az keyvault certificate create -n lab-cert -p @labs/keyvault/lab/lab-policy.json --vault-name <kv-name> 
```


作成には時間がかかりますが、出力には証明書署名要求（CSR）が表示されますが、証明書の詳細は表示されません。`az keyvault certificate download` コマンドは公開キーのみをダウンロードします。公開キーと秘密キーの両方をエクスポートするには、シークレットをダウンロードする必要があります:


```
az keyvault secret download -f lab-cert.pfx --name lab-cert --vault-name <kv-name> 
```


> これはPFXファイルで、OpenSSLなどのツールを使用して公開および秘密の証明書ファイルに分割できます。

KeyVaultで通常のシークレットとして証明書が表示されないことに注意してください:



```
# ここでは証明書が表示されません
az keyvault secret list --vault-name <kv-name> 
```
