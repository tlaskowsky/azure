# ラボの解決策

デフォルトのNSGルールは、VNet内の任意のポートへのトラフィックを許可します - これはピアされたVNetsにも適用されます。

デフォルトを削除することはできませんが、デフォルトよりも優先度が高い新しいルールを作成して、優先順位を上げることができます：

- ポート80で10.20.x.xの範囲内のIPからの受信トラフィックを許可するルール
- その他のすべての受信VNetトラフィックを拒否するルール

そして、デフォルトよりも優先度が高いVNetのための新しいルールを追加します：



```
# すべてのVNetアクセスをブロック：
az network nsg rule create -g labs-vnet-access --nsg-name nsg01 -n 'BlockIncomingVnet' --direction Inbound --access Deny --priority 400 --source-address-prefixes 'VirtualNetwork' --destination-port-ranges '*'
  
# vm02のシェルセッションからテストします - 新しいルールが有効になるまで数分かかりますが、それからは失敗するはずです：
curl --connect-timeout 2 <vm01-private-ip-address>
```

```
# 10.20のアドレスからのアクセスを許可：
az network nsg rule create -g labs-vnet-access --nsg-name nsg01 -n 'AllowSubnet2' --direction Inbound --access Allow --priority 300 --source-address-prefixes '10.20.0.0/16' --destination-port-ranges '80'

# vm02のシェルセッションからテストします - ルールが適用されると、これが再び機能するようになります：
curl --connect-timeout 2 <vm01-private-ip-address>
```
