package wtf.sheashaa.bus;

import android.content.ClipData;
import android.content.Context;
import android.content.Intent;
import android.content.SharedPreferences;
import android.os.AsyncTask;
import android.provider.ContactsContract;
import android.provider.Settings;
import android.support.design.widget.FloatingActionButton;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.util.Log;
import android.view.View;
import android.widget.AdapterView;
import android.widget.ArrayAdapter;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Spinner;
import android.widget.TextView;
import android.widget.Toast;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.OutputStream;
import java.net.InetAddress;
import java.net.InetSocketAddress;
import java.net.Socket;

public class MainBusActivity extends AppCompatActivity {

    public static final String PREFS_NAME = "BusClient";
    public static final int DEFAULT_PORT = 3800;
    private static final int BUFFER_SIZE = 1024;

    Spinner RouteSpinner;
    Spinner BusSpinner;

    EditText NameEditText;
    EditText EMailEditText;
    EditText PhoneEditText;


    Button BookButton;
    FloatingActionButton SettingsButton;
    FloatingActionButton ConnectButton;

    String IPAddress;
    int Port;

    SharedPreferences settings;

    String route;
    String bus;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_main_bus);

        RouteSpinner = (Spinner) findViewById(R.id.route_spinner);
        BusSpinner = (Spinner) findViewById(R.id.bus_spinner);

        ArrayAdapter<CharSequence> adapter = ArrayAdapter.createFromResource(this,
                R.array.routes, android.R.layout.simple_spinner_item);
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        RouteSpinner.setAdapter(adapter);

        adapter = ArrayAdapter.createFromResource(this,
                R.array.buses, android.R.layout.simple_spinner_item);
        adapter.setDropDownViewResource(android.R.layout.simple_spinner_dropdown_item);
        BusSpinner.setAdapter(adapter);

        NameEditText = (EditText) findViewById(R.id.name_et);
        EMailEditText = (EditText) findViewById(R.id.email_et);
        PhoneEditText = (EditText) findViewById(R.id.phone_et);

        BookButton = (Button) findViewById(R.id.book_btn);
        SettingsButton = (FloatingActionButton) findViewById(R.id.fab);
        ConnectButton = (FloatingActionButton) findViewById(R.id.fab2);

        settings = getSharedPreferences(PREFS_NAME, 0);
        IPAddress = settings.getString("IP", "");
        Port = settings.getInt("Port", DEFAULT_PORT);

        if (IPAddress == "") {
            Toast.makeText(getApplicationContext(),"Network is not configured", Toast.LENGTH_SHORT).show();
        }

        SettingsButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                Intent intent = new Intent(MainBusActivity.this, BusActivity.class);
                startActivity(intent);
            }
        });

        ConnectButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if (IPAddress == "") {
                    Toast.makeText(getApplicationContext(),"Network is not configured", Toast.LENGTH_SHORT).show();
                    return;
                }
                new ConnectTry().execute();
            }
        });

        BookButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                if (IPAddress == "") {
                    Toast.makeText(getApplicationContext(),"Network is not configured", Toast.LENGTH_SHORT).show();
                }

                if (route == "" || bus == ""){
                    Toast.makeText(getApplicationContext(),"Fill missing information", Toast.LENGTH_SHORT).show();
                    return;
                }
                new SendAsync().execute(prepare());
            }
        });

        RouteSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
                 route = parent.getItemAtPosition(position).toString();
            }

            @Override
            public void onNothingSelected(AdapterView<?> parent) {
                route = "";
            }
        });

        BusSpinner.setOnItemSelectedListener(new AdapterView.OnItemSelectedListener() {
            @Override
            public void onItemSelected(AdapterView<?> parent, View view, int position, long id) {
                bus = parent.getItemAtPosition(position).toString();
            }

            @Override
            public void onNothingSelected(AdapterView<?> parent) {
                bus = "";
            }
        });
    }

    private String prepare() {
        String data = "";

        String name = NameEditText.getText().toString();
        String email = EMailEditText.getText().toString();
        String phone = PhoneEditText.getText().toString();
        data = name + "," + email + "," + phone + "\n" + route + "\n" + bus;

        return data;
    }

    public boolean runJavaSocket(String msg) {
        settings = getSharedPreferences(PREFS_NAME, 0);
        IPAddress = settings.getString("IP", "");
        Port = settings.getInt("Port", DEFAULT_PORT);

        if (IPAddress == "") {
            Toast.makeText(getApplicationContext(),"Network is not configured", Toast.LENGTH_SHORT).show();
            return false;
        }

        boolean ServerAlive = serverListening(IPAddress, Port);
        if (!ServerAlive) return false;

        ByteArrayOutputStream stream = new ByteArrayOutputStream();
        ByteArrayInputStream rdr = new ByteArrayInputStream(msg.getBytes());

        byte[] buffer = new byte[BUFFER_SIZE];

        try{
            Socket socket = new Socket(InetAddress.getByName(IPAddress), Port);

            OutputStream output = socket.getOutputStream();
            String string = "HEADER\n" + Integer.toString(msg.length()) + "\n";
            System.arraycopy(string.getBytes("US-ASCII"), 0, buffer, 0, string.length());

            output.write(buffer);

            int count;
            while ((count = rdr.read(buffer,0,buffer.length)) > 0) {
                output.write(buffer, 0, count);
            }

            /* Flush the output to commit */
            output.flush();

            return true;
        }
        catch (Exception e){
            Log.e("Client", "exception", e);
            return false;
        }
    }

    public boolean serverListening(String host, int port, boolean prompt)
    {
        Socket s = new Socket();
        try
        {
            s.connect(new InetSocketAddress(host, port), 1000);
            return true;
        }
        catch (Exception e)
        {
            if (prompt) Toast.makeText(getApplicationContext(),e.getMessage(),Toast.LENGTH_SHORT).show();
            return false;
        }
        finally
        {
            try {s.close();}
            catch(Exception ignored){}
        }
    }

    public boolean serverListening(String host, int port){
        return serverListening(host, port, false);
    }


    private class ConnectTry extends AsyncTask<Integer,Integer,Boolean> {
        @Override
        protected Boolean doInBackground(Integer... integers) {
            return serverListening(IPAddress, Port, false);
        }

        @Override
        protected void onPostExecute(Boolean result) {
            if (result) Toast.makeText(getApplicationContext(), "Connected",Toast.LENGTH_SHORT).show();
            else Toast.makeText(getApplicationContext(), "Connection Failed",Toast.LENGTH_SHORT).show();
        }
    }

    private class SendAsync extends AsyncTask<String,Integer,Boolean> {
        @Override
        protected Boolean doInBackground(String[] strings) {
            return runJavaSocket(strings[0]);
        }

        @Override
        protected void onPostExecute(Boolean result) {
            Toast.makeText(getApplicationContext(), "File sending " + (result ? "" : "un") + "successful", Toast.LENGTH_SHORT).show();
        }
    }
}
