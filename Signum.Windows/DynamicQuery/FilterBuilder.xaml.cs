﻿using System;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Linq;
using System.Collections.ObjectModel;
using Signum.Entities;
using System.Windows.Input;
using System.Collections.Generic;
using System.Globalization;
using Signum.Utilities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Reflection;

namespace Signum.Windows
{
	public partial class FilterBuilder
	{  
        ObservableCollection<FilterOption> filters;
        public ObservableCollection<FilterOption> Filters
        {
            get { return filters; }
            set
            {
                filters = value;
                lvFilters.ItemsSource = filters;
            }
        }

        List<Column> columns;
        public List<Column> Columns
        {
            get { return columns; }
            set
            {
                columns = value;
                cbFilters.ItemsSource = value;
            }
        }

        public FilterBuilder()
        {
            this.InitializeComponent();
        }

        private void btCreate_Click(object sender, RoutedEventArgs e)
        {
            if (cbFilters.SelectedItem == null)
            {
                MessageBox.Show(Properties.Resources.NoFilterSelected);
                return;
            }

            Column c = (Column)cbFilters.SelectedItem;
            FilterType ft = FilterOperationsUtils.GetFilterType(c.Type);
            FilterOption f = new FilterOption
            {
                Column = c,
                Operation =  FilterOperationsUtils.FilterOperations[ft].First(),
                Value = null,
            };

            filters.Add(f); 
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            FilterOption f = (FilterOption)cb.DataContext;
            FilterType ft = FilterOperationsUtils.GetFilterType(f.Column.Type);
            cb.ItemsSource = FilterOperationsUtils.FilterOperations[ft];
            cb.IsEnabled = !f.Frozen;
        }

        private void Grid_Loaded(object sender, RoutedEventArgs e)
        {
            Grid g = (Grid)sender;
            Common.SetCurrentWindow(g, this.FindCurrentWindow());
            FilterOption f = (FilterOption)g.DataContext;

            Common.SetIsReadOnly(g, f.Frozen); 

            Type type = f.Column.Type;
            if (typeof(Lite).IsAssignableFrom(type) || typeof(IdentifiableEntity).IsAssignableFrom(type))
            {
                Type cleanType = typeof(Lite).IsAssignableFrom(type) ? Reflector.ExtractLite(type) : type;
                if (Reflector.IsLowPopulation(cleanType))
                {
                    EntityCombo ec = new EntityCombo { Type = type, Style = (Style)FindResource("toolTip") };
                    ec.SetBinding(EntityCombo.EntityProperty, new Binding
                    {
                        Path = new PropertyPath(FilterOption.RealValueProperty),
                        NotifyOnValidationError = true,
                        ValidatesOnDataErrors = true,
                        ValidatesOnExceptions = true,
                    });
                    g.Children.Add(ec);
                }
                else
                {
                    EntityLine el = new EntityLine { Type = type, Create = false, HideAutoCompleteOnLostFocus = false };
                    el.SetBinding(EntityLine.EntityProperty, new Binding
                    {
                        Path = new PropertyPath(FilterOption.RealValueProperty),
                        NotifyOnValidationError = true,
                        ValidatesOnDataErrors = true,
                        ValidatesOnExceptions = true
                    });
                    g.Children.Add(el);
                }
            }
            else
            {
                ValueLine vl = new ValueLine() { Type = type, Format = f.Column.Format, UnitText = f.Column.Unit };
                vl.SetBinding(ValueLine.ValueProperty, new Binding
                {
                    Path = new PropertyPath("RealValue"),
                    NotifyOnValidationError = true,
                    ValidatesOnDataErrors = true,
                    ValidatesOnExceptions = true,
                    Converter = Reflector.IsNumber(type) ?  Converters.Identity: null,
                });
                g.Children.Add(vl);
            }
        }

        private void btRemove_Click(object sender, RoutedEventArgs e)
        {
            RemoveFilters();
        }

        private void RemoveFilters()
        {
            var toRemove = lvFilters.SelectedItems.Cast<FilterOption>().ToList();
            foreach (var f in toRemove)
            {
                filters.Remove(f);
            }
        }

        private void lvFilters_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                RemoveFilters();
            }
        }

        private void lvFilters_DragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(typeof(FilterOption)) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void lvFilters_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(FilterOption)))
            {
                FilterOption filter = (FilterOption)e.Data.GetData(typeof(FilterOption));

                filters.Add(filter); 
            }
        }
	}
}