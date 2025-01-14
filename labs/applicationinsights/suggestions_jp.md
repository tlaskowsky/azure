# ラボの提案

現在のアーキテクチャを使用している限り、Application Insights からのすべてのデータは Log Analytics に保存されます。別々の App Insights アプリを使用すると、特定のアプリケーションに合わせた体験が得られます - UI では一つのアプリのみを扱い、フィルタリングせずにメトリクスや関連イベントを確認できます。各 App Insights インスタンスが独自の Log Analytics ワークスペースを使用する場合、2つの異なるワークスペース間で結合する必要があるため、クエリはより困難（そして遅く）なります。

反対のアプローチは、すべてのコンポーネントに対して一つの App Insights と一つの Log Analytics ワークスペースを持つことです。これにより、依存関係を追跡し、完全なユーザーワークフローを確認できますが、特定のコンポーネントのパフォーマンスを見たい場合には UI からフィルタリングする必要があります。

中間的なアプローチは、複数の App Insights が中央の Log Analytics ワークスペースに書き込むことです。これにより、App Insights の UI で焦点を絞った体験を得ながらも、Log Analytics でアプリケーション全体のビューを得ることができます。
