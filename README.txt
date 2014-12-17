Dependencies
Install-Package MahApps.Metro
Install-Package Rx-Linq 

Website
http://github.com/superlloyd/MenuRibbon

About Ribbon
http://msdn.microsoft.com/en-us/library/dn742393.aspx
http://msdn.microsoft.com/EN-US/library/hh140095(v=VS.110,d=hv.2).aspx
http://msdn.microsoft.com/en-us/library/ff701790(v=vs.110).aspx

// MS UI Hierarchy
RibbonMenu
	RibbonTab
		RibbonGroup
			RibbonButton, RibbonRadioButton, RibbonToggleButton, RibbonCheckBox, RibbonTextBox
			RibbonComboBox, RibbonMenuButton, RibbonSplitButton
			RibbonControlGroup

// ME: UI Hierarchy
MenuRibbon
	MenuItem
		Separator
		MenuItemContainer/Any
		MenuItem 
			Any
			MenuItem
	RibbonItem
		Any
		RibbonBar 
			RibbonGroup
				Any
				ItemsButton
					Any
					MenuItem

// ME: Class Hierarchy
HeaderedItemsControl
	ActionHeaderedItemsControl (Command, IsChecked, Icon, Click)
		MenuItem ([PopupItem], Root, Role, ParentItem, IsOpen, IsHovering/Highlighted)
			ItemsButton ([PopupRoot], IsSplit, ShowHeader, ControlSizeDefinition)
ItemsControl
	RibbonBar
	MenuRibbon ([PopupRoot])
	RibbonBar
HeaderedContentControl
	RibbonItem ([PopupItem])


Keyboard Handling Tips:
MS Ribbon Sample: KeyTipService, InputManager.Current.(Pre/Post)ProcessInput
		InputManager.Current.PostProcessInput += new ProcessInputEventHandler(PostProcessInput);
		InputManager.Current.PreProcessInput += new PreProcessInputEventHandler(PreProcessInput);
			if (e.StagingItem.Input.RoutedEvent == Keyboard.PreviewKeyUpEvent)
MS (Menu)Item: OnKeyDown +> handle tab and arrow, move focus
		KeyboardNavigation.ControlTabNavigationProperty.OverrideMetadata(typeof(Menu), new FrameworkPropertyMetadata(KeyboardNavigationMode.Once));
		KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(Menu), new FrameworkPropertyMetadata(KeyboardNavigationMode.Cycle));
		EventManager.RegisterClassHandler(typeof(Menu), AccessKeyManager.AccessKeyPressedEvent, new AccessKeyPressedEventHandler(OnAccessKeyPressed));
KeyTipService singleton
Keyboard.Focus(null); => restore keyboard to outside FocusScope

+ nav from floating ribbon
+ nav from button (why twice down?)