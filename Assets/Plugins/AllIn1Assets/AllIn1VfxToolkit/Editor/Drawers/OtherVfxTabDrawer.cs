namespace AllIn1Assets.AllIn1VfxToolkit
{
	public class OtherVfxTabDrawer : AssetWindowTabDrawer
	{
		private const string TAB_NAME = "Other";

		private AllIn1VfxWindow vfxWindow;

		public OtherVfxTabDrawer(CommonStyles commonStyles, AllIn1CommonWindow parentWindow, AbstractPluginConfig abstractPluginConfig) : base(commonStyles, parentWindow, abstractPluginConfig)
		{
			vfxWindow = (AllIn1VfxWindow)parentWindow;
		}

		public override void Draw()
		{
			vfxWindow.OtherTab();
		}

		public override void EnteredPlayMode()
		{

		}

		public override string GetTabName()
		{
			return TAB_NAME;
		}

		public override void Hide()
		{

		}

		public override void OnDisable()
		{

		}

		public override void OnEnable()
		{

		}

		public override void Show()
		{

		}
	}
}
