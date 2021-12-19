# Marduk.Controls
C# port for Marduk PhotowallView and WaterfallFlowView controls for UWP

(https://github.com/ProjectMarduk/Marduk.Controls)

Example:
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
