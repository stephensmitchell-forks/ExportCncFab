﻿using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.ApplicationServices;
using System.IO;
using System.Collections.Generic;

namespace ExportCncFab
{
  /// <summary>
  /// Shared parameters to keep track of 
  /// the CNC fabrication export history.
  /// </summary>
  class ExportParameters
  {
    const string _is_exported = "CncFabIsExported";
    const string _exported_first = "CncFabExportedFirst";
    const string _exported_last = "CncFabExportedLast";

    //Guid _guid_is_exported;
    //Guid _guid_exported_first;
    //Guid _guid_exported_last;

    Definition _definition_is_exported = null;
    Definition _definition_exported_first = null;
    Definition _definition_exported_last = null;

    Document _doc = null;
    List<ElementId> _ids = null;

    static bool SetDefinition(
      ref Definition definition,
      Element e,
      string parameter_name )
    {
      Parameter p = e.get_Parameter( parameter_name );

      bool rc = ( null != p );

      if( rc )
      {
        definition = p.Definition;
      }

      return rc;
    }

    /// <summary>
    /// Initialise the shared parameter definitions 
    /// from a given sample element.
    /// </summary>
    /// <param name="e"></param>
    public ExportParameters( Element e )
    {
      bool rc
        = SetDefinition( ref _definition_is_exported,
          e, _is_exported )
        && SetDefinition( ref _definition_exported_first,
          e, _exported_first )
        && SetDefinition( ref _definition_exported_last,
          e, _exported_last );

      if( rc )
      {
        _doc = e.Document;
        _ids = new List<ElementId>();
      }
    }

    /// <summary>
    /// Check whether all CNC fabrication export 
    /// parameter definitions were successfully 
    /// initialised.
    /// </summary>
    public bool IsValid
    {
      get
      {
        return null != _definition_is_exported
          && null != _definition_exported_first
          && null != _definition_exported_last;
      }
    }

    /// <summary>
    /// Add a part element id to the list of
    /// successfully exported parts.
    /// </summary>
    public void Add( ElementId id )
    {
      _ids.Add( id );
    }

    /// <summary>
    /// Update the CNC fabrication export 
    /// history for the given element.
    /// </summary>
    void UpdateExportHistory( 
      Element e )
    {
      DateTime now = DateTime.Now;

      string s = string.Format( 
        "{0:4}-{1:02}-{2:02}T{3:02}.{4:02}.{5:02}.{6:03}",
        now.Year, now.Month, now.Day, 
        now.Hour, now.Minute, now.Second, now.Millisecond );

      s = now.ToString( "yyyy-MM-ddTHH:mm:ss.fff" );

      e.get_Parameter( _definition_is_exported )
        .Set( 1 );

      Parameter p = e.get_Parameter( 
        _definition_exported_first );

      string s2 = p.AsString();

      if( null == s2 || 0 == s2.Length )
      {
        p.Set( s );
      }

      e.get_Parameter( _definition_exported_last )
        .Set( s );
    }

    /// <summary>
    /// Update the CNC fabrication export 
    /// history for all stored element ids.
    /// </summary>
    public void UpdateExportHistory()
    {
      foreach( ElementId id in _ids )
      {
        UpdateExportHistory( 
          _doc.GetElement( id ) );
      }
    }

    /// <summary>
    /// Create the shared parameters to keep track
    /// of the CNC fabrication export history.
    /// </summary>
    public static void Create( Document doc )
    {
      /// <summary>
      /// Shared parameters filename; used only in case
      /// none is set and we need to create the export
      /// history shared parameters.
      /// </summary>
      const string _shared_parameters_filename
        = "export_cnc_fab_shared_parameters.txt";

      const string _definition_group_name = "CncFab";

      Application app = doc.Application;

      // Retrieve shared parameter file name

      string sharedParamsFileName
        = app.SharedParametersFilename;

      if( null == sharedParamsFileName
        || 0 == sharedParamsFileName.Length )
      {
        string path = Path.GetTempPath();

        path = Path.Combine( path, 
          _shared_parameters_filename );

        StreamWriter stream;
        stream = new StreamWriter( path );
        stream.Close();

        app.SharedParametersFilename = path;

        sharedParamsFileName
          = app.SharedParametersFilename;
      }

      // Retrieve shared parameter file object

      DefinitionFile f
        = app.OpenSharedParameterFile();

      using( Transaction t = new Transaction( doc ) )
      {
        t.Start( "Create CNC Export Tracking "
          + "Shared Parameters" );

        // Create the category set for binding

        CategorySet catSet = app.Create.NewCategorySet();

        Category cat = doc.Settings.Categories.get_Item(
          BuiltInCategory.OST_Parts );

        catSet.Insert( cat );

        Binding binding = app.Create.NewInstanceBinding(
          catSet );

        // Retrieve or create shared parameter group

        DefinitionGroup group
          = f.Groups.get_Item( _definition_group_name )
          ?? f.Groups.Create( _definition_group_name );

        // Retrieve or create the three parameters;
        // we could check if they are already bound, 
        // but it looks like Insert will just ignore 
        // them in that case.

        Definition definition
          = group.Definitions.get_Item( _is_exported )
          ?? group.Definitions.Create( _is_exported,
            ParameterType.YesNo, true );

        doc.ParameterBindings.Insert( definition, binding,
          BuiltInParameterGroup.PG_GENERAL );

        definition
          = group.Definitions.get_Item( _exported_first )
          ?? group.Definitions.Create( _exported_first,
            ParameterType.Text, true );

        doc.ParameterBindings.Insert( definition, binding,
          BuiltInParameterGroup.PG_GENERAL );

        definition
          = group.Definitions.get_Item( _exported_last )
          ?? group.Definitions.Create( _exported_last,
            ParameterType.Text, true );

        doc.ParameterBindings.Insert( definition, binding,
          BuiltInParameterGroup.PG_GENERAL );

        t.Commit();
      }
    }
  }
}