
## 設定可能なアプリの公開

イングレスオブジェクトはローカルネームスペース内のサービスを参照するため、アプリと同じネームスペースにイングレスを作成する必要があります：

- [configurable-http.yaml](solution/ingress/configurable-http.yaml)



```
kubectl apply -f labs/kubernetes/ingress/solution/ingress
```


新しいドメインなので、ホストファイルに追加する必要があります：



```
# on Windows:
./scripts/add-to-hosts.ps1 configurable.local 127.0.0.1

# on *nix:
./scripts/add-to-hosts.sh configurable.local 127.0.0.1
```


> これで http://configurable.local:8000 にアクセスできます。

## 標準のHTTPおよびHTTPSポートの使用

これには、イングレスコントローラーLoadBalancerサービスの公開ポートを変更するだけです：

- [controller/service-lb.yaml](solution/controller/service-lb.yaml)



```
kubectl apply -f labs/kubernetes/ingress/solution/controller

kubectl get svc -n ingress-nginx
```


これで通常のURLを使用できます：

- http://configurable.local
- http://pi.local
- http://localhost
- https://pi.local（TLS証明書が信頼されていないというエラーが表示されます）

## LoadBalancerサービスをサポートしていないクラスターでは、なぜこれを行うことができないのですか？

NodePortsは特権ポート範囲以外の30000+に制限されています。NodePortを80または443でリッスンさせることはできません。

> [演習](README_jp.md)に戻る。
