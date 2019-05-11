package wtf.sheashaa.bus;

import android.Manifest;
import android.content.SharedPreferences;
import android.content.pm.PackageManager;
import android.os.Build;
import android.support.v4.app.ActivityCompat;
import android.support.v4.content.ContextCompat;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;
import android.widget.EditText;
import android.widget.Toast;

public class BusActivity extends AppCompatActivity {

    public static final String PREFS_NAME = "BusClient";
    public static final int DEFAULT_PORT = 3800;

    EditText IPEditText;
    EditText PortEditText;

    Button SaveButton;
    Button CancelButton;

    SharedPreferences settings;

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        setContentView(R.layout.activity_bus);

        IPEditText = (EditText) findViewById(R.id.ip_et);
        PortEditText = (EditText) findViewById(R.id.port_et);

        SaveButton = (Button) findViewById(R.id.save_btn);
        CancelButton = (Button) findViewById(R.id.cancel_btn);

         settings = getSharedPreferences(PREFS_NAME, 0);

         IPEditText.setText(settings.getString("IP", "").toString());
         PortEditText.setText(Integer.toString(settings.getInt("Port", DEFAULT_PORT)));

        SaveButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                savePreferences();
                finish();
            }
        });

        CancelButton.setOnClickListener(new View.OnClickListener() {
            @Override
            public void onClick(View v) {
                finish();
            }
        });
    }

    private void savePreferences()
    {
        settings = getSharedPreferences(PREFS_NAME, 0);
        SharedPreferences.Editor editor = settings.edit();

        try {
            String IPAddress = IPEditText.getText().toString();
            int Port = Integer.parseInt(PortEditText.getText().toString());
            if (Port <= 0 || Port >= 65534) throw new Exception("Invalid Port");

            editor.putString("IP", IPAddress);
            editor.putInt("Port", Port);
        }
        catch(Exception e){
            Toast.makeText(getApplicationContext(),e.getMessage(),Toast.LENGTH_SHORT).show();
            return;
        }

        editor.commit();
        Toast.makeText(getApplicationContext(), "Preferences Saved", Toast.LENGTH_SHORT).show();
    }
}
