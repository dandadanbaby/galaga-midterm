import javax.imageio.ImageIO;
import javax.swing.*;
import java.awt.*;
import java.awt.event.*;
import java.awt.geom.AffineTransform;
import java.awt.image.BufferedImage;
import java.io.File;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.Random;

public class Main extends JPanel implements ActionListener, KeyListener {

    static final int W = 900, H = 700;
    static final int MAX_LEVEL = 5;
    static final int START_LIVES = 3;
    static final Random RNG = new Random();

    enum State { MENU, INTRO, PLAYING, PAUSED, GAME_OVER, VICTORY }

    static class Assets {
        static final Map<String, BufferedImage> images = new HashMap<>();
        static final Map<String, Boolean> soundExists = new HashMap<>();

        static BufferedImage img(String name) {
            if (images.containsKey(name)) return images.get(name);
            BufferedImage bi = null;
            try {
                File f = new File("assets/" + name);
                if (f.exists()) bi = ImageIO.read(f);
            } catch (Exception ignored) { }
            images.put(name, bi);
            return bi;
        }

    }

    static abstract class Entity {
        double x, y, vx, vy, angle;
        double radius;
        boolean dead;

        void move() {
            x += vx; y += vy;
            if (x < -radius) { x = W + radius;}
            if (x > W + radius) { x = -radius;}
            if (y < -radius) { y = H + radius;}
            if (y > H + radius) { y = -radius;}
        }

        boolean hits(Entity o) {
            double dx = x - o.x, dy = y - o.y, r = radius + o.radius;
            return dx * dx + dy * dy < r * r;
        }
    }

    static class Ship extends Entity {
        boolean thrusting;
        int invulnerable = 180;
        int cooldown;

        Ship() { reset(); }

        void reset() {
            x = W / 2.0; y = H / 2.0; vx = vy = 0; angle = 0;
            radius = 14; dead = false; invulnerable = 180;
        }
    }

    static class Bullet extends Entity {
        int life = 60;
        Bullet(Ship s) {
            radius = 3;
            angle = s.angle;
            double sx = Math.sin(angle), sy = -Math.cos(angle);
            x = s.x + sx * 18; y = s.y + sy * 18;
            vx = s.vx + sx * 9; vy = s.vy + sy * 9;
        }
        @Override void move() { super.move(); if (--life <= 0) dead = true; }
    }

    static class Asteroid extends Entity {
        int size;         
        boolean boss;
        int hp = 1;
        double spin;
        double[] shape;      

        Asteroid(double x, double y, int size, double speed) {
            this.x = x; this.y = y; this.size = size;
            radius = size == 3 ? 42 : size == 2 ? 24 : 12;
            double dir = RNG.nextDouble() * Math.PI * 2;
            double sp = speed * (0.6 + RNG.nextDouble() * 0.8) * (4 - size) * 0.7;
            vx = Math.cos(dir) * sp; vy = Math.sin(dir) * sp;
            spin = (RNG.nextDouble() - 0.5) * 0.04;
            angle = RNG.nextDouble() * Math.PI * 2;
            makeShape(12);
        }

        static Asteroid boss(double speed) {
            Asteroid a = new Asteroid(RNG.nextBoolean() ? 60 : W - 60, 60, 3, speed);
            a.boss = true; a.hp = 30; a.radius = 80;
            double d = RNG.nextDouble() * Math.PI * 2;
            a.vx = Math.cos(d) * 0.9; a.vy = Math.sin(d) * 0.9;
            a.spin = 0.006;
            a.makeShape(18);
            return a;
        }

        void makeShape(int n) {
            shape = new double[n];
            for (int i = 0; i < n; i++) shape[i] = 0.75 + RNG.nextDouble() * 0.3;
        }

        @Override void move() { super.move(); angle += spin; }
    }

    static class Particle {
        double x, y, vx, vy; int life, maxLife; Color color;
        Particle(double x, double y, Color c) {
            this.x = x; this.y = y; color = c;
            double d = RNG.nextDouble() * Math.PI * 2, s = 0.5 + RNG.nextDouble() * 3.5;
            vx = Math.cos(d) * s; vy = Math.sin(d) * s;
            maxLife = life = 20 + RNG.nextInt(30);
        }
    }

    static class Star { double x, y, speed; int size; }

    State state = State.MENU;
    Ship ship = new Ship();
    final List<Bullet> bullets = new ArrayList<>();
    final List<Asteroid> asteroids = new ArrayList<>();
    final List<Particle> particles = new ArrayList<>();
    final Star[] stars = new Star[120];
    final boolean[] keys = new boolean[256];

    int level = 1, score = 0, lives = START_LIVES;
    int introTimer = 0, respawnTimer = 0;
    int highScore = 0;
    Asteroid bossRef = null;
    final javax.swing.Timer timer;

    public Main() {
        setPreferredSize(new Dimension(W, H));
        setBackground(Color.BLACK);
        setFocusable(true);
        addKeyListener(this);
        for (int i = 0; i < stars.length; i++) {
            Star s = new Star();
            s.x = RNG.nextInt(W); s.y = RNG.nextInt(H);
            s.speed = 0.1 + RNG.nextDouble() * 0.5; s.size = 1 + RNG.nextInt(2);
            stars[i] = s;
        }
        timer = new javax.swing.Timer(16, this);
        timer.start();
    }

    void newGame() {
        level = 1; score = 0; lives = START_LIVES;
        startLevel();
    }

    void startLevel() {
        bullets.clear(); asteroids.clear(); particles.clear();
        ship.reset();
        bossRef = null;
        double speed = 0.9 + (level - 1) * 0.25;
        int large = level == MAX_LEVEL ? 3 : 2 + level;   // 3,4,5,6 then boss + 3
        for (int i = 0; i < large; i++) asteroids.add(spawnAwayFromShip(3, speed));
        if (level == MAX_LEVEL) {
            bossRef = Asteroid.boss(speed);
            asteroids.add(bossRef);
        }
        introTimer = 150;
        state = State.INTRO;
    }

    Asteroid spawnAwayFromShip(int size, double speed) {
        double x, y;
        do {
            x = RNG.nextInt(W); y = RNG.nextInt(H);
        } while (Math.hypot(x - W / 2.0, y - H / 2.0) < 220);
        return new Asteroid(x, y, size, speed);
    }

    @Override public void actionPerformed(ActionEvent e) {
        for (Star s : stars) { s.y += s.speed; if (s.y > H) { s.y = 0; s.x = RNG.nextInt(W); } }
        switch (state) {
            case INTRO:
                updateParticles();
                if (--introTimer <= 0) state = State.PLAYING;
                break;
            case PLAYING:
                updatePlaying();
                break;
            default:
                break;
        }
        repaint();
    }

    void updatePlaying() {
        if (!ship.dead) {
            if (keys[KeyEvent.VK_LEFT] || keys[KeyEvent.VK_A]) ship.angle -= 0.075;
            if (keys[KeyEvent.VK_RIGHT] || keys[KeyEvent.VK_D]) ship.angle += 0.075;
            ship.thrusting = keys[KeyEvent.VK_UP] || keys[KeyEvent.VK_W];
            if (ship.thrusting) {
                ship.vx += Math.sin(ship.angle) * 0.18;
                ship.vy -= Math.cos(ship.angle) * 0.18;
                if (RNG.nextInt(2) == 0) {
                    Particle p = new Particle(ship.x - Math.sin(ship.angle) * 14,
                            ship.y + Math.cos(ship.angle) * 14, new Color(255, 160, 40));
                    p.vx = ship.vx - Math.sin(ship.angle) * 2; p.vy = ship.vy + Math.cos(ship.angle) * 2;
                    p.life = p.maxLife = 12;
                    particles.add(p);
                }
            }
            double sp = Math.hypot(ship.vx, ship.vy);
            if (sp > 7) { ship.vx *= 7 / sp; ship.vy *= 7 / sp; }
            ship.vx *= 0.99; ship.vy *= 0.99;
            ship.move();
            if (ship.invulnerable > 0) ship.invulnerable--;
            if (ship.cooldown > 0) ship.cooldown--;
            if (keys[KeyEvent.VK_SPACE] && ship.cooldown == 0) {
                bullets.add(new Bullet(ship));
                ship.cooldown = 12;
            }
        } else if (--respawnTimer <= 0) {
            if (lives > 0) ship.reset();
            else { endGame(State.GAME_OVER); return; }
        }

        for (Bullet b : bullets) b.move();
        for (Asteroid a : asteroids) a.move();
        updateParticles();

        // only a nigger would do this honestly
        List<Asteroid> spawned = new ArrayList<>();
        for (Bullet b : bullets) {
            if (b.dead) continue;
            for (Asteroid a : asteroids) {
                if (!a.dead && b.hits(a)) {
                    b.dead = true;
                    hitAsteroid(a, spawned);
                    break;
                }
            }
        }
        asteroids.addAll(spawned);

        if (!ship.dead && ship.invulnerable <= 0) {
            for (Asteroid a : asteroids) {
                if (!a.dead && ship.hits(a)) { killShip(); break; }
            }
        }

        bullets.removeIf(b -> b.dead);
        asteroids.removeIf(a -> a.dead);

        if (asteroids.isEmpty()) {
            score += 500 * level;
            if (level >= MAX_LEVEL) endGame(State.VICTORY);
            else { level++; startLevel(); }
        }
    }

    void hitAsteroid(Asteroid a, List<Asteroid> spawned) {
        if (a.boss) {
            a.hp--;
            score += 50;
            burst(a.x, a.y, 6, new Color(255, 200, 120));
            if (a.hp > 0 && a.hp % 10 == 0) {     
                double sp = 0.9 + (level - 1) * 0.25;
                spawned.add(new Asteroid(a.x, a.y, 2, sp));
                spawned.add(new Asteroid(a.x, a.y, 2, sp));
            }
            if (a.hp <= 0) {
                a.dead = true; score += 5000;
                burst(a.x, a.y, 120, new Color(255, 120, 40));
            }
            return;
        }
        a.dead = true;
        score += a.size == 3 ? 20 : a.size == 2 ? 50 : 100;
        burst(a.x, a.y, 10 + a.size * 6, new Color(200, 200, 200));
        if (a.size > 1) {
            double sp = 0.9 + (level - 1) * 0.25;
            for (int i = 0; i < 2; i++) spawned.add(new Asteroid(a.x, a.y, a.size - 1, sp * 1.2));
        }
    }

    void killShip() {
        ship.dead = true;
        lives--;
        respawnTimer = 90;
        burst(ship.x, ship.y, 50, new Color(120, 200, 255));

    }

    void endGame(State s) {
        state = s;
        highScore = Math.max(highScore, score);
    }

    void burst(double x, double y, int n, Color c) {
        for (int i = 0; i < n; i++) particles.add(new Particle(x, y, c));
    }

    void updateParticles() {
        for (Particle p : particles) { p.x += p.vx; p.y += p.vy; p.life--; }
        particles.removeIf(p -> p.life <= 0);
    }

    @Override protected void paintComponent(Graphics g0) {
        super.paintComponent(g0);
        Graphics2D g = (Graphics2D) g0;
        g.setRenderingHint(RenderingHints.KEY_ANTIALIASING, RenderingHints.VALUE_ANTIALIAS_ON);
        g.setRenderingHint(RenderingHints.KEY_INTERPOLATION, RenderingHints.VALUE_INTERPOLATION_BILINEAR);

        drawBackground(g);

        if (state != State.MENU) {
            for (Asteroid a : asteroids) drawAsteroid(g, a);
            for (Bullet b : bullets) drawBullet(g, b);
            if (!ship.dead) drawShip(g);
            for (Particle p : particles) {
                float alpha = Math.max(0f, Math.min(1f, p.life / (float) p.maxLife));
                g.setColor(new Color(p.color.getRed(), p.color.getGreen(), p.color.getBlue(), (int) (alpha * 255)));
                g.fillOval((int) p.x - 1, (int) p.y - 1, 3, 3);
            }
            drawHud(g);
        }
        drawOverlay(g);
    }

    void drawBackground(Graphics2D g) {
        BufferedImage bg = Assets.img("background.png");
        if (bg != null) {
            g.drawImage(bg, 0, 0, W, H, null);
        } else {
            g.setPaint(new GradientPaint(0, 0, new Color(5, 5, 20), 0, H, new Color(20, 5, 40)));
            g.fillRect(0, 0, W, H);
        }
        g.setColor(new Color(255, 255, 255, 170));
        for (Star s : stars) g.fillRect((int) s.x, (int) s.y, s.size, s.size);
    }

    void drawSprite(Graphics2D g, BufferedImage img, double x, double y, double angle, double diameter) {
        AffineTransform old = g.getTransform();
        g.translate(x, y);
        g.rotate(angle);
        double scale = diameter / Math.max(img.getWidth(), img.getHeight());
        g.scale(scale, scale);
        g.drawImage(img, -img.getWidth() / 2, -img.getHeight() / 2, null);
        g.setTransform(old);
    }

    void drawShip(Graphics2D g) {
        // blink while invulnerable
        if (ship.invulnerable > 0 && (ship.invulnerable / 6) % 2 == 0) return;
        BufferedImage img = Assets.img("ship.png");
        if (img != null) {
            drawSprite(g, img, ship.x, ship.y, ship.angle, 44);
            return;
        }
        AffineTransform old = g.getTransform();
        g.translate(ship.x, ship.y);
        g.rotate(ship.angle);
        if (ship.thrusting) {
            g.setColor(new Color(255, 150, 30));
            g.fillPolygon(new int[]{-6, 0, 6}, new int[]{12, 12 + 8 + RNG.nextInt(10), 12}, 3);
        }
        Polygon p = new Polygon(new int[]{0, 12, 0, -12}, new int[]{-18, 14, 8, 14}, 4);
        g.setColor(new Color(90, 170, 255));
        g.fillPolygon(p);
        g.setColor(Color.WHITE);
        g.drawPolygon(p);
        g.setTransform(old);
    }

    void drawBullet(Graphics2D g, Bullet b) {
        BufferedImage img = Assets.img("bullet.png");
        if (img != null) { drawSprite(g, img, b.x, b.y, b.angle, 14); return; }
        g.setColor(new Color(255, 255, 120));
        g.fillOval((int) b.x - 3, (int) b.y - 3, 6, 6);
    }

    void drawAsteroid(Graphics2D g, Asteroid a) {
        String name = a.boss ? "boss.png" : a.size == 3 ? "asteroid_large.png"
                : a.size == 2 ? "asteroid_medium.png" : "asteroid_small.png";
        BufferedImage img = Assets.img(name);
        if (img != null) {
            drawSprite(g, img, a.x, a.y, a.angle, a.radius * 2);
        } else {
            int n = a.shape.length;
            Polygon p = new Polygon();
            for (int i = 0; i < n; i++) {
                double t = a.angle + i * Math.PI * 2 / n;
                p.addPoint((int) (a.x + Math.cos(t) * a.radius * a.shape[i]),
                           (int) (a.y + Math.sin(t) * a.radius * a.shape[i]));
            }
            g.setColor(a.boss ? new Color(110, 50, 50) : new Color(90, 85, 80));
            g.fillPolygon(p);
            g.setColor(a.boss ? new Color(255, 120, 80) : new Color(190, 185, 175));
            g.setStroke(new BasicStroke(a.boss ? 3f : 1.5f));
            g.drawPolygon(p);
            g.setStroke(new BasicStroke(1f));
        }
    }

    void drawHud(Graphics2D g) {
        g.setFont(new Font("Monospaced", Font.BOLD, 18));
        g.setColor(Color.WHITE);
        g.drawString("SCORE " + score, 20, 28);
        g.drawString("LEVEL " + level + "/" + MAX_LEVEL, W / 2 - 55, 28);
        g.drawString("HI " + highScore, W - 130, 28);

        for (int i = 0; i < lives; i++) {
            BufferedImage img = Assets.img("ship.png");
            if (img != null) drawSprite(g, img, 30 + i * 28, 56, 0, 22);
            else {
                g.setColor(new Color(90, 170, 255));
                int cx = 30 + i * 28;
                g.fillPolygon(new int[]{cx, cx + 8, cx, cx - 8}, new int[]{44, 62, 58, 62}, 4);
            }
        }
        if (bossRef != null && !bossRef.dead) {
            int bw = 300, bx = (W - bw) / 2, by = H - 36;
            g.setColor(new Color(60, 0, 0));
            g.fillRect(bx, by, bw, 14);
            g.setColor(new Color(220, 50, 40));
            g.fillRect(bx, by, (int) (bw * bossRef.hp / 30.0), 14);
            g.setColor(Color.WHITE);
            g.drawRect(bx, by, bw, 14);
            g.setFont(new Font("Monospaced", Font.BOLD, 13));
            g.drawString("MOTHER ASTEROID", bx + 90, by - 5);
        }
    }

    void center(Graphics2D g, String text, int y, Font f, Color c) {
        g.setFont(f); g.setColor(c);
        int w = g.getFontMetrics().stringWidth(text);
        g.drawString(text, (W - w) / 2, y);
    }

    void drawOverlay(Graphics2D g) {
        Font big = new Font("SansSerif", Font.BOLD, 64);
        Font mid = new Font("SansSerif", Font.BOLD, 26);
        Font small = new Font("SansSerif", Font.PLAIN, 18);
        switch (state) {
            case MENU:
                center(g, "SPACE ASTEROIDS", 230, big, new Color(120, 200, 255));
                center(g, "Survive 5 levels of asteroids. Defeat the Mother Asteroid.", 290, small, Color.LIGHT_GRAY);
                center(g, "PRESS ENTER TO START", 400, mid, Color.WHITE);
                center(g, "Arrows / WASD: move     Space: fire     P: pause", 470, small, Color.GRAY);
                if (highScore > 0) center(g, "High score: " + highScore, 520, small, Color.YELLOW);
                break;
            case INTRO:
                center(g, "LEVEL " + level, H / 2 - 10, big, Color.WHITE);
                center(g, level == MAX_LEVEL ? "BOSS INCOMING!" : "Get ready...", H / 2 + 40, mid,
                        level == MAX_LEVEL ? new Color(255, 90, 70) : Color.LIGHT_GRAY);
                break;
            case PAUSED:
                shade(g);
                center(g, "PAUSED", H / 2, big, Color.WHITE);
                center(g, "Press P to resume", H / 2 + 45, small, Color.LIGHT_GRAY);
                break;
            case GAME_OVER:
                shade(g);
                center(g, "GAME OVER", H / 2 - 20, big, new Color(255, 80, 70));
                center(g, "Score: " + score + "   Reached level " + level, H / 2 + 30, mid, Color.WHITE);
                center(g, "Press ENTER to return to menu", H / 2 + 75, small, Color.LIGHT_GRAY);
                break;
            case VICTORY:
                shade(g);
                center(g, "VICTORY!", H / 2 - 20, big, new Color(120, 255, 140));
                center(g, "You cleared the asteroid field. Final score: " + score, H / 2 + 30, mid, Color.WHITE);
                center(g, "Press ENTER to return to menu", H / 2 + 75, small, Color.LIGHT_GRAY);
                break;
            default:
                break;
        }
    }

    void shade(Graphics2D g) {
        g.setColor(new Color(0, 0, 0, 150));
        g.fillRect(0, 0, W, H);
    }

    @Override public void keyPressed(KeyEvent e) {
        int k = e.getKeyCode();
        if (k >= 0 && k < keys.length) keys[k] = true;
        if (k == KeyEvent.VK_ESCAPE) System.exit(0);
        if (k == KeyEvent.VK_ENTER) {
            if (state == State.MENU) newGame();
            else if (state == State.GAME_OVER || state == State.VICTORY) state = State.MENU;
        }
        if (k == KeyEvent.VK_P) {
            if (state == State.PLAYING) state = State.PAUSED;
            else if (state == State.PAUSED) state = State.PLAYING;
        }
    }

    @Override public void keyReleased(KeyEvent e) {
        int k = e.getKeyCode();
        if (k >= 0 && k < keys.length) keys[k] = false;
    }

    @Override public void keyTyped(KeyEvent e) { }

    public static void main(String[] args) {
        SwingUtilities.invokeLater(() -> {
            JFrame f = new JFrame("Space Asteroids");
            f.setDefaultCloseOperation(JFrame.EXIT_ON_CLOSE);
            f.setResizable(false);
            f.add(new Main());
            f.pack();
            f.setLocationRelativeTo(null);
            f.setVisible(true);
        });
    }
}