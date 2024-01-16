# ラボの解決策

オリジングループ - 同じプロパティで、名前のみが変わります：



```
az afd origin-group create -g labs-frontdoor --origin-group-name pi-web --profile-name labs --probe-request-type GET --probe-protocol Http    --probe-interval-in-seconds 30 --probe-path /  --sample-size 4  --successful-samples-required 3 --additional-latency-in-milliseconds 50
```


オリジン - 今回は1つだけです：



```
az container show -g labs-frontdoor --name pi --query 'ipAddress.fqdn'

az afd origin create -g labs-frontdoor --profile-name labs --origin-group-name pi-web --origin-name container1 --priority 1 --weight 100 --enabled-state Enabled  --http-port 80 --origin-host-header <pi-fqdn> --host-name <pi-fqdn>
```


エンドポイント：



```
az afd endpoint create -g labs-frontdoor --profile-name labs --endpoint-name pi-web --enabled-state Enabled
```


ルートを作成 - これによりアプリが公開されます：



```
az afd route create -g labs-frontdoor --profile-name labs --endpoint-name pi-web --forwarding-protocol HttpOnly --route-name spi-web-route --origin-group pi-web --supported-protocols Http --https-redirect Disabled --link-to-default-domain Enabled --enable-compression
```


ポータルで確認します。新しい設定がプロビジョニングされているのを見ることができ、Piエンドポイントにアクセスできるはずです。
