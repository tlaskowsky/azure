**Labの提案**

Bitnami RabbitMQのマーケットプレイス提供は、実際にはカスタムVMイメージです。VMイメージ名を見つけ、標準の `az vm create` コマンドを使用して直接VMを作成できます。その後、スクリプトアクションを使用してファイルからRabbitMQのユーザー名とパスワードを取得します。

また、[rabbitmqadmin](https://www.rabbitmq.com/management-cli.html) CLIを使用してキューを作成することもできます。
