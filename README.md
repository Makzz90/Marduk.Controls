# Marduk.Controls
C# port of Marduk PhotowallView and WaterfallFlowView controls for UWP (original - https://github.com/ProjectMarduk/Marduk.Controls)

![waterfall](https://cloud.githubusercontent.com/assets/9367842/17103352/8064d730-52b0-11e6-865d-bdd07396ed0c.gif)

Example:
```XAML
<ScrollViewer>
      <marduc:WaterfallFlowView ItemSource="{Binding Items}" AdaptiveMode="MinBased" MinItemWidth="400" DelayMeasure="True"
                                Spacing="15">
          <marduc:WaterfallFlowView.ItemTemplate>
              <DataTemplate>
                  <Border Background="{Binding Brush}" HorizontalAlignment="Stretch">
                    <TextBlock FontSize="50" Text="{Binding Num}" HorizontalAlignment="Center" VerticalAlignment="Center"/>
                </Border>
              </DataTemplate>
          </marduc:WaterfallFlowView.ItemTemplate>
      </marduc:WaterfallFlowView>
  </ScrollViewer>
```
