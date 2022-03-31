# GETMAN

## ゲーム説明

キャラクターを操作し、ステージ内に出現する星型のアイテム(ポイントアイテム)を集めるゲームです。
<br>3つ集めるとステージクリアです。
<br>特定の時間のタイミングでアイテムとモンスターが出現します。
<br>プレイヤーはモンスターを攻撃したり避けながらアイテムを集めていきます。
<br>プレイヤーのHPが無くなったり、右上の制限時間が0になるとゲームオーバーです。

### 操作方法
↑↓←→キー、またはWASDキーで上下左右に移動
Spaceキーで攻撃

## 主な実装機能
- GM
  - ゲームの開始
  - ゲームクリア
  - ゲームオーバー
  - セーブ(スコア、ステージ番号)
- タイマー
  - 制限時間
- プレイヤー
  - 移動
  - 剣での攻撃
  - アニメーション
- エネミー
  - ステージの移動
  - プレイヤーの追跡
  - アニメーション
- ステージ管理
  - ステージの切り替え
  - ステージクリアの監視
- ステージ
  - ポイントアイテムの自動生成
  - エネミーの自動生成
  - ギミックの配置
- UI
  - HP、獲得ポイント数、獲得スコア、制限時間、ステージ番号
  - ゲーム開始、ゲームクリア、ゲームオーバー
  - シーン移動時のフェード
  - ダイアログ
- etc...

## 技術目標
- UniRxを使ってみる
- MV(R)Pパターンでゲームを制作してみる
- Zenjectを使用してDIをしてみる
- 機能をアセンブリ単位で区切って管理してみる
- NavMesh機能の復習
- 3D操作の復習
## クラス図
<p><a href="/UML.md" target="_blank">クラス図はこちらへ</a></p>

## 反省
- UniRxとUniTaskのどちらで機能を実装するかの線引きが曖昧だったため、予めルール決めをしてどちらで実装するか考えておけば良かったと反省(複雑な処理は可読性の点でUniTaskで実装するなど)
- アセンブリとnamespaceの両方で機能を区切ったのは冗長だったかもしれない。
- スケジュール管理が甘く、当初に予定していた機能の一部を削ってしまったため、徹底的に管理しておけばもっと多くの機能が実装できたかもしれない。
- 最初から丁寧に作ろうと意識しすぎたあまり、考えすぎてしまい時間がかかってしまうことも多々あった。作業の手戻りも考慮して先に全体を作りつつ、後から細かい部分を作っていけば良かった。

## 感想
- 前回制作したRoleBattleの反省点として、クラス設計を考慮して制作することはうまく反省を活かせたかなと思いました。
- 面白いゲームを制作するという点ではまだまだ研究が足りないと思っています。今後も課題として考えながら制作していきたいです。

## 今後追加したい機能
- ゲームのBGM, SEの音量調整機能
- ステージの追加
- 新たなエネミーの追加
- 課金機能を実装し、新たな武器を獲得する
- マルチ機能による複数人での協力orバトル
- スコアによるランキング機能