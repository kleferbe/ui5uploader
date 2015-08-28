"! HTTP Handler for the UI5Upload Webservice. Installs the UI5 application contained in a ZIP file uploaded in the body of the HTTP request.
"! Some form parameters can be given to control the upload behaviour. Parameters that are given overwrite the corresponding settings in the .Ui5RepositoryUploadParameters.
"!
"! Parameters:
"! sapui5applicationname: Name of the Application.
"! sapui5applicationdescription: Description of the Application. Used only when the application is created.
"! sapui5applicationpackage: Packaged to deploy the application to.
"! workbenchrequest: Transport request to deploy the application with. can be omitted if the sapui5applicationpackage is "$TMP".
"! externalcodepage: You should use "utf-8" here.
"! acceptunixstyleeol: &lt;true|false&gt; Triggers automatic conversion of unix style text files to the Windows format expected by the repository.
"! deltamode: &lt;true|false&gt; Only changes to current state in the repository get registered in workbench request.
"! testmode: &lt;true|false&gt; Triggers a test run in which no upload takes place. The HTTP response holds a detailed log then.
CLASS z_btc_ui5_upload_webservice DEFINITION
PUBLIC FINAL CREATE PUBLIC .
  PUBLIC SECTION.
    INTERFACES if_http_extension.
  PROTECTED SECTION.
  PRIVATE SECTION.
    "! TTL of the ZIP-Data in the HTTP cache in seconds.
    CONSTANTS zip_cache_ttl_s TYPE i VALUE 180. "3 minutes
    DATA url_parameters TYPE tihttpnvp.

    "! Uploads the ZIPs binary data to the http cache with a timeout of zip_cache_livetime_s seconds and returns the URL to the cached ZIP.
    METHODS upload_zip_to_http_cache
      IMPORTING i_zip_data TYPE xstring
      RETURNING VALUE(r_url) TYPE string.

    "! Returns the value of the URL parameter specified by i_parameter_name
    "! @parameter i_parameter_name | Name of the URL parameter to retrieve
    "! @parameter r_result | The value of the URL parameter or INITIAL if the URL Parameter is not existent.
    METHODS get_url_parameter
      IMPORTING i_parameter_name TYPE string
      RETURNING VALUE(r_result) TYPE string.
ENDCLASS.



CLASS z_btc_ui5_upload_webservice IMPLEMENTATION.
  METHOD if_http_extension~handle_request.
    if server->request->get_method( ) = 'HEAD'.
       server->response->set_status( code = 200 reason = 'OK' ).
       return.
    endif.
    if server->request->get_method( ) <> 'POST'.
       server->response->set_status( code = 500 reason = 'Only POST supported.' ).
      return.
    endif.
    server->request->get_form_fields( CHANGING fields = url_parameters ).
    data(zip_url) = upload_zip_to_http_cache( server->request->get_data( ) ).

    DATA success TYPE char1.
    DATA log_messages TYPE string_table.
    try.
    CALL FUNCTION '/UI5/UI5_REPOSITORY_LOAD_HTTP'
      EXPORTING
        iv_url                     = zip_url
        iv_sapui5_application_name = get_url_parameter( `sapui5applicationname` )
        iv_sapui5_application_desc = get_url_parameter( `sapui5applicationdescription` )
        iv_package                 = conv devclass( get_url_parameter( `sapui5applicationpackage` ) )
        iv_workbench_request       = conv trkorr( get_url_parameter( `workbenchrequest` ) )
        iv_external_code_page      = get_url_parameter( `externalcodepage` )
        iv_accept_unix_style_eol   = switch #( get_url_parameter( `acceptunixstyleeol` ) when `true` then abap_true when `false` then abap_false else abap_undefined )
        iv_delta_mode              = switch #( get_url_parameter( `deltamode` ) when `true` then abap_true when `false` then abap_false else abap_undefined )
        iv_test_mode               = switch #( get_url_parameter( `testmode` ) when `true` then abap_true else abap_false )
      IMPORTING
        ev_success                 = success
        ev_log_messages            = log_messages.
    catch cx_root into data(e).
        success = 'E'.
        while e is bound.
            e->get_source_position( importing program_name = data(program) include_name = data(include) source_line = data(line) ).
            insert |{ cl_abap_classdescr=>get_class_name( e ) }: { e->get_text( ) } in { include } { program } line { line }| into table log_messages.
            e = e->previous.
        endwhile.
    endtry.
    case success.
      when 'E'.
        server->response->set_status( code = 500 reason = 'Installation of UI5 application failed.' ).
      when 'S'.
        server->response->set_status( code = 200 reason = 'Finished' ).
      when others.
        server->response->set_status( code = 206 reason = 'Finished with warnings' ).
    endcase.

    CONCATENATE LINES OF log_messages INTO data(log_string) SEPARATED BY cl_abap_char_utilities=>newline.
    server->response->set_cdata( data = log_string ).
  ENDMETHOD.

  METHOD get_url_parameter.
    IF line_exists( url_parameters[ name = i_parameter_name ] ).
      r_result = url_parameters[ name = i_parameter_name ]-value.
    ENDIF.
  ENDMETHOD.

  METHOD upload_zip_to_http_cache.
    DATA(cached_response) = NEW cl_http_response( add_c_msg = 1 ).
    cached_response->set_content_type( `application/zip` ).
    cached_response->set_data( i_zip_data ).
    cached_response->if_http_response~set_status( code = 200 reason = 'OK' ).
    cached_response->if_http_response~server_cache_expire_rel( zip_cache_ttl_s ).
    cl_wd_utilities=>construct_wd_url( EXPORTING application_name = `UI5Upload` IMPORTING out_absolute_url = r_url ).

    r_url = r_url && `/` && cl_system_uuid=>create_uuid_c32_static( ).
    cl_http_server=>server_cache_upload( response = cached_response url = r_url ).
  ENDMETHOD.

ENDCLASS.