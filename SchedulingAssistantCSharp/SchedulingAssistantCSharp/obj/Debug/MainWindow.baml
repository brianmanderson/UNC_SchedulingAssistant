   M S B A M L   `   `   `  ����T  PSchedulingAssistantCSharp, Version=1.0.0.0, Culture=neutral, PublicKeyToken=nullE'clr-namespace:SchedulingAssistantCSharpSchedulingAssistantCSharp  5   	   *    $SchedulingAssistantCSharp.MainWindow   5   	   R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   9local'clr-namespace:SchedulingAssistantCSharp     5   	     � Title$  
MainWindow��5   	   $	��450��6   $	��800��6)   .��5	      �  � ColumnDefinitions 5
   
   �� 5      $��3* �6   �� 5      $��7* �6   .+�5   
   &   System.Windows.Controls.Calendar  -   5          Name$ calendarControl��5      $��10q�6,   68   �� 5   
   $��1��6   $��10q�6%   .+�5      �� $��Scheduled Tasks��6   $��16(�6/   $
��Bold"�6=   $��0,0,0,10q�6O   �� 5       �� Orientation$ 
Horizontal=�6   $��Right=�62   $��0,0,0,10q�6N   .+�5      �� -   5         ��Name$ comboBoxTaskDefinitions��5      $	��200��6<    �� DisplayMemberPath$
 Name��6H   �� 5      -   5         ��Name$ 
btnAddTask��5      $��Add Task��6-   $��5,0,0,0q�6Y   5      �� 5      -   5         ��Name$ listBoxScheduledTasks��5      $	��325��65   ��5      �� 5      .R�5      �� $ 
Horizontal=�6%   .+�5       �� $	��200��6C   ��6(   �� *	Task.Name+6C   �� 5!      $��10,0,0,0q�6i   ��6(   �� *ScheduledDate+ �� StringFormat$  - {0:MM/dd/yyyy}��6i   �� 5#      -   5        $��Delete��5#   %   $��10,0,0,0q�66   
	 � Tag	 6g   �� 5$      5%      5'      5(      �� 5*   
   -   5        $ btnCreateGeneralTasks��5*      $��Create general tasks��60   $
��Left=�5+      $��Bottom=�6,   $��
10,0,0,106q�6G   5,      �� 5-   
   -   5        $ btnCreatePersonnel��5-      $��Create Personnel��6-   $
��Left=�5.      $��Bottom=�6,   $��	19,0,0,40q�6G   5/      �� 50   
   -   5        $ btnSaveSchedule��50      $��Save Schedule��6*   $��Center=�51      $	��Top=�6*   $��	0,238,0,0q�6B   52      53      54      